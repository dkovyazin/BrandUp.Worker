using BrandUp.Worker.Allocator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor
{
    public class TaskExecutor : IJobExecutorContext
    {
        #region Fields

        public static readonly Type HandlerBaseType = typeof(TaskHandler<>);
        private readonly ITaskAllocator taskAllocator;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<TaskExecutor> logger;
        private IExecutorConnection executorConnection;
        private bool isStarted = false;
        private int executedCommands = 0;
        private int faultedCommands = 0;
        private int cancelledCommands = 0;
        private readonly ConcurrentDictionary<Guid, TaskJob> _startedJobs = new ConcurrentDictionary<Guid, TaskJob>();

        #endregion

        #region Properties

        public bool IsStarted => isStarted;
        public int ExecutingCommands => _startedJobs.Count;
        public int ExecutedCommands => executedCommands;
        public int FaultedCommands => faultedCommands;
        public int CancelledCommands => cancelledCommands;

        #endregion

        public TaskExecutor(ITaskAllocator taskAllocator, IServiceProvider serviceProvider, ILogger<TaskExecutor> logger)
        {
            this.taskAllocator = taskAllocator ?? throw new ArgumentNullException(nameof(taskAllocator));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task WorkAsync(IExecutorConnection executorConnection, CancellationToken cancellationToken)
        {
            if (isStarted)
                throw new InvalidOperationException();
            isStarted = true;

            this.executorConnection = executorConnection ?? throw new ArgumentNullException(nameof(executorConnection));

            await WaitingTasksCycleAsync(cancellationToken);

            isStarted = false;
        }

        private async Task WaitingTasksCycleAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Executor {executorConnection.ExecutorId} start working.");

            var countWaits = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation($"Executor {executorConnection.ExecutorId} waiting tasks...");

                TaskToExecute[] commandsToExecute;

                try
                {
                    commandsToExecute = (await taskAllocator.WaitTasksAsync(ExecutorId, cancellationToken)).ToArray();
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, $"Executor {executorConnection.ExecutorId} error waiting tasks.");

                    await Task.Delay(5000);

                    continue;
                }

                logger.LogInformation($"Executor {executorConnection.ExecutorId} received tasks: {commandsToExecute.Length}");

                foreach (var taskToExecute in commandsToExecute)
                    StartJob(taskToExecute, cancellationToken);

                countWaits++;
            }

            logger.LogInformation($"Executor {executorConnection.ExecutorId} end working.");
        }
        private void StartJob(TaskToExecute taskToExecute, CancellationToken cancellationToken)
        {
            if (!executorConnection.TryGetHandlerMetadata(taskToExecute.TaskModel.GetType(), out TaskHandlerMetadata handlerMetadata))
                throw new InvalidOperationException();

            var jobScope = serviceProvider.CreateScope();

            try
            {
                var taskHandler = (ITaskHandler)jobScope.ServiceProvider.GetRequiredService(handlerMetadata.HandlerType);
                var jobLogger = jobScope.ServiceProvider.GetRequiredService<ILogger<TaskJob>>();

                var job = new TaskJob(taskToExecute.TaskId, taskToExecute.TaskModel, taskHandler, this, jobLogger);
                if (!_startedJobs.TryAdd(job.TaskId, job))
                    throw new InvalidOperationException("Не удалось добавить задачу в список выполняемых.");

                job.Start(cancellationToken, taskToExecute.Timeout);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                jobScope.Dispose();
            }
        }
        private void DetachJob(TaskJob job)
        {
            if (!_startedJobs.TryRemove(job.TaskId, out TaskJob removed))
                throw new InvalidOperationException();
            removed.Dispose();
        }

        #region IJobExecutorContext members

        public Guid ExecutorId => executorConnection.ExecutorId;
        async Task IJobExecutorContext.OnSuccessJob(TaskJob job)
        {
            DetachJob(job);

            await taskAllocator.SuccessTaskAsync(ExecutorId, job.TaskId, job.Elapsed, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
        }
        async Task IJobExecutorContext.OnDefferJob(TaskJob job)
        {
            DetachJob(job);

            await taskAllocator.DeferTaskAsync(ExecutorId, job.TaskId, CancellationToken.None);

            Interlocked.Increment(ref cancelledCommands);
        }
        async Task IJobExecutorContext.OnTimeoutJob(TaskJob job)
        {
            DetachJob(job);

            await taskAllocator.ErrorTaskAsync(ExecutorId, job.TaskId, job.Elapsed, new TimeoutException("Timeout task executing."), CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
            Interlocked.Increment(ref faultedCommands);
        }
        async Task IJobExecutorContext.OnErrorJob(TaskJob job, Exception exception)
        {
            DetachJob(job);

            await taskAllocator.ErrorTaskAsync(ExecutorId, job.TaskId, job.Elapsed, exception, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
            Interlocked.Increment(ref faultedCommands);
        }
        async Task IJobExecutorContext.OnUnhandledError(TaskJob job, Exception exception)
        {
            DetachJob(job);

            await taskAllocator.ErrorTaskAsync(ExecutorId, job.TaskId, job.Elapsed, exception, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
            Interlocked.Increment(ref faultedCommands);
        }

        #endregion
    }

    public interface IExecutorConnection
    {
        Guid ExecutorId { get; }
        bool TryGetHandlerMetadata(Type taskType, out TaskHandlerMetadata handlerMetadata);
    }

    public class TaskHandlerMetadata
    {
        public string TaskName { get; }
        public Type HandlerType { get; }

        public TaskHandlerMetadata(string taskName, Type handlerType)
        {
            TaskName = taskName;
            HandlerType = handlerType;
        }
    }
}