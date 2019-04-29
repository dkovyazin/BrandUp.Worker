using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor
{
    internal class TaskJob : IDisposable
    {
        private readonly object taskModel;
        private readonly ITaskHandler taskHandler;
        private readonly IJobExecutorContext executorContext;
        private readonly ILogger<TaskJob> logger;
        private CancellationTokenSource cancellationSource;
        private Stopwatch executionWatch;
        private int timeoutInMilliseconds = 0;

        public Guid TaskId { get; }
        public TimeSpan Elapsed => executionWatch.Elapsed;

        internal TaskJob(Guid taskId, object taskModel, ITaskHandler taskHandler, IJobExecutorContext executorContext, ILogger<TaskJob> logger)
        {
            TaskId = taskId;
            this.taskModel = taskModel;
            this.taskHandler = taskHandler;
            this.executorContext = executorContext;
            this.logger = logger;
        }

        public void Start(CancellationToken cancellationToken, int timeoutInMilliseconds)
        {
            if (timeoutInMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutInMilliseconds));
            this.timeoutInMilliseconds = timeoutInMilliseconds;

            executionWatch = Stopwatch.StartNew();

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationSource.CancelAfter(timeoutInMilliseconds);

            logger.LogInformation($"Task {TaskId} starting.");

            Task.Run(ExecuteAsync, cancellationSource.Token);
        }

        private async Task ExecuteAsync()
        {
            logger.LogInformation($"Task {TaskId} started.");

            try
            {
                try
                {
                    await taskHandler.WorkAsync(taskModel, cancellationSource.Token);

                    executionWatch.Stop();

                    logger.LogInformation($"Task {TaskId} success.");

                    await executorContext.OnSuccessJob(this);
                }
                catch (OperationCanceledException)
                {
                    executionWatch.Stop();

                    if (executionWatch.ElapsedMilliseconds >= timeoutInMilliseconds)
                    {
                        logger.LogWarning($"Task {TaskId} execution timeout.");

                        await executorContext.OnTimeoutJob(this);
                    }
                    else
                    {
                        logger.LogInformation($"Task {TaskId} cancelled.");

                        await executorContext.OnDefferJob(this);
                    }
                }
                catch (Exception exception)
                {
                    executionWatch.Stop();

                    logger.LogError(exception, $"Task {TaskId} error.");

                    await executorContext.OnErrorJob(this, exception);
                }
            }
            catch (Exception unhandledException)
            {
                logger.LogCritical(unhandledException, $"Task {TaskId} unhandled error.");

                await executorContext.OnUnhandledError(this, unhandledException);
            }
        }

        public void Dispose()
        {
            taskHandler?.Dispose();
            cancellationSource?.Dispose();
        }
    }

    internal interface IJobExecutorContext
    {
        Guid ExecutorId { get; }
        Task OnSuccessJob(TaskJob job);
        Task OnDefferJob(TaskJob job);
        Task OnTimeoutJob(TaskJob job);
        Task OnErrorJob(TaskJob job, Exception exception);
        Task OnUnhandledError(TaskJob job, Exception exception);
    }
}