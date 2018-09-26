using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor
{
    public class JobTask : IDisposable
    {
        public Guid TaskId { get; }
        private readonly object taskModel;
        private readonly ITaskHandler taskHandler;
        private readonly IJobExecutorContext executorContext;
        private CancellationTokenSource cancellationSource;
        private Stopwatch executionWatch;
        private int timeoutInMilliseconds = 0;

        public TimeSpan Elapsed => executionWatch.Elapsed;

        internal JobTask(Guid commandId, object command, ITaskHandler handler, IJobExecutorContext executorContext)
        {
            TaskId = commandId;
            taskModel = command;
            taskHandler = handler;
            this.executorContext = executorContext;
        }

        public void Start(CancellationToken cancellationToken, int timeoutInMilliseconds)
        {
            if (timeoutInMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutInMilliseconds));
            this.timeoutInMilliseconds = timeoutInMilliseconds;

            executionWatch = Stopwatch.StartNew();

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationSource.CancelAfter(timeoutInMilliseconds);

            Task.Run(ExecuteAsync, cancellationSource.Token);
        }

        private async Task ExecuteAsync()
        {
            try
            {
                try
                {
                    await taskHandler.WorkAsync(taskModel, cancellationSource.Token);

                    executionWatch.Stop();

                    await executorContext.OnSuccessJob(this);
                }
                catch (OperationCanceledException)
                {
                    executionWatch.Stop();

                    if (executionWatch.ElapsedMilliseconds >= timeoutInMilliseconds)
                        await executorContext.OnTimeoutJob(this);
                    else
                        await executorContext.OnCancelledJob(this);
                }
                catch (Exception exception)
                {
                    executionWatch.Stop();

                    await executorContext.OnErrorJob(this, exception);
                }
            }
            catch (Exception unhandledException)
            {
                await executorContext.OnUnhandledError(this, unhandledException);
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            taskHandler?.Dispose();
            cancellationSource?.Dispose();
        }
    }

    public interface IJobExecutorContext
    {
        Guid ExecutorId { get; }
        Task OnSuccessJob(JobTask job);
        Task OnCancelledJob(JobTask job);
        Task OnTimeoutJob(JobTask job);
        Task OnErrorJob(JobTask job, Exception exception);
        Task OnUnhandledError(JobTask job, Exception exception);
    }
}