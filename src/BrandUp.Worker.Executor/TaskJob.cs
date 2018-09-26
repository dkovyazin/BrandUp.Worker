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

        public TimeSpan Elapsed => executionWatch.Elapsed;

        internal JobTask(Guid commandId, object command, ITaskHandler handler, IJobExecutorContext executorContext)
        {
            TaskId = commandId;
            taskModel = command;
            taskHandler = handler;
            this.executorContext = executorContext;
        }

        public void Start(CancellationToken cancellationToken)
        {
            executionWatch = Stopwatch.StartNew();

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task.Factory.StartNew(Start, cancellationSource.Token, TaskCreationOptions.None, TaskScheduler.Current);
        }

        private void Start()
        {
            using (var task = ExecuteAsync())
            {
                task.Wait(cancellationSource.Token);

                if (task.IsFaulted)
                    throw new InvalidOperationException();
            }
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
            if (taskHandler != null)
                taskHandler.Dispose();

            if (cancellationSource != null)
                cancellationSource.Dispose();
        }
    }

    public interface IJobExecutorContext
    {
        Guid ExecutorId { get; }
        Task OnSuccessJob(JobTask job);
        Task OnCancelledJob(JobTask job);
        Task OnErrorJob(JobTask job, Exception exception);
        Task OnUnhandledError(JobTask job, Exception exception);
    }
}
