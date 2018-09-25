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
        private CancellationTokenSource _cancellation;
        private Stopwatch _stopwatch;

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        internal JobTask(Guid commandId, object command, ITaskHandler handler, IJobExecutorContext executorContext)
        {
            TaskId = commandId;
            taskModel = command;
            taskHandler = handler;
            this.executorContext = executorContext;
        }

        public void Start(CancellationToken cancellationToken)
        {
            _stopwatch = Stopwatch.StartNew();

            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task.Factory.StartNew(WorkWait, _cancellation.Token, TaskCreationOptions.None, TaskScheduler.Current);
        }

        private void WorkWait()
        {
            _cancellation.Token.ThrowIfCancellationRequested();

            using (var task = WorkHandlerAsync())
            {
                task.Wait(_cancellation.Token);

                if (task.IsFaulted)
                    throw new InvalidOperationException();
                else if (task.IsCanceled)
                {
                    if (!_cancellation.IsCancellationRequested)
                        throw new InvalidOperationException();
                }
            }
        }

        private async Task WorkHandlerAsync()
        {
            try
            {
                await taskHandler.WorkAsync(taskModel, _cancellation.Token);

                _stopwatch.Stop();

                await executorContext.OnSuccessJob(this);
            }
            catch (OperationCanceledException)
            {
                _stopwatch.Stop();

                await executorContext.OnCancelledJob(this);
            }
            catch (Exception exception)
            {
                _stopwatch.Stop();

                await executorContext.OnErrorJob(this, exception);
            }
        }

        public void Dispose()
        {
            if (taskHandler != null)
                taskHandler.Dispose();

            if (_cancellation != null)
                _cancellation.Dispose();
        }
    }

    public interface IJobExecutorContext
    {
        Guid ExecutorId { get; }
        Task OnSuccessJob(JobTask job);
        Task OnCancelledJob(JobTask job);
        Task OnErrorJob(JobTask job, Exception exception);
    }
}
