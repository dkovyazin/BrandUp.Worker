using BrandUp.Worker.Allocator;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor
{
    public class TaskJob : IDisposable
    {
        private readonly object taskModel;
        private readonly ITaskHandler taskHandler;
        private readonly IJobExecutorContext executorContext;
        private CancellationTokenSource cancellationSource;
        private Stopwatch executionWatch;
        private readonly int timeoutInMilliseconds = 0;

        public Guid TaskId { get; }
        public TimeSpan Elapsed => executionWatch.Elapsed;

        public TaskJob(TaskExecutionModel executionModel, TaskHandlerMetadata handlerMetadata, IServiceScope serviceScope, IJobExecutorContext executorContext)
        {
            if (executionModel == null)
                throw new ArgumentNullException(nameof(executionModel));
            if (handlerMetadata == null)
                throw new ArgumentNullException(nameof(handlerMetadata));
            if (executionModel.Timeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(executionModel.Timeout));

            TaskId = executionModel.TaskId;
            taskModel = executionModel.TaskModel;
            timeoutInMilliseconds = executionModel.Timeout;

            taskHandler = (ITaskHandler)serviceScope.ServiceProvider.GetRequiredService(handlerMetadata.HandlerType);
            this.executorContext = executorContext ?? throw new ArgumentNullException(nameof(executorContext));
        }

        public Task Run(CancellationToken cancellationToken)
        {
            executionWatch = Stopwatch.StartNew();

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationSource.CancelAfter(timeoutInMilliseconds);

            return Task.Run(ExecuteAsync, cancellationSource.Token);
        }

        private async Task ExecuteAsync()
        {
            try
            {
                await executorContext.OnStartedJob(this);

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
                        await executorContext.OnDefferJob(this);
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
        }

        public void Dispose()
        {
            if (taskHandler is IDisposable d)
                d.Dispose();

            cancellationSource?.Dispose();
        }
    }

    public interface IJobExecutorContext
    {
        Guid ExecutorId { get; }
        Task OnStartedJob(TaskJob job);
        Task OnSuccessJob(TaskJob job);
        Task OnDefferJob(TaskJob job);
        Task OnTimeoutJob(TaskJob job);
        Task OnErrorJob(TaskJob job, Exception exception);
        Task OnUnhandledError(TaskJob job, Exception exception);
    }
}