using BrandUp.Worker.Allocator;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private bool isStarted = false;
        private int executedCommands = 0;
        private int faultedCommands = 0;
        private int cancelledCommands = 0;
        private readonly ConcurrentDictionary<Guid, TaskJob> _startedJobs = new ConcurrentDictionary<Guid, TaskJob>();
        private readonly Dictionary<Type, TaskHandlerMetadata> handlerFactories = new Dictionary<Type, TaskHandlerMetadata>();

        #endregion

        #region Properties

        public bool IsStarted => isStarted;
        public int ExecutingCommands => _startedJobs.Count;
        public int ExecutedCommands => executedCommands;
        public int FaultedCommands => faultedCommands;
        public int CancelledCommands => cancelledCommands;

        #endregion

        public TaskExecutor(ITaskAllocator taskAllocator, ITaskHandlerLocator handlerLocator, ITaskMetadataManager metadataManager, IServiceProvider serviceProvider, ILogger<TaskExecutor> logger)
        {
            this.taskAllocator = taskAllocator ?? throw new ArgumentNullException(nameof(taskAllocator));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            foreach (var taskType in handlerLocator.TaskTypes)
            {
                var taskMetadata = metadataManager.FindTaskMetadata(taskType);
                if (taskMetadata == null)
                    throw new InvalidOperationException();

                handlerFactories.Add(taskType, new TaskHandlerMetadata(taskMetadata.TaskName, HandlerBaseType.MakeGenericType(taskType)));
            }
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Connecting executor...");

            ExecutorId = await taskAllocator.SubscribeAsync(handlerFactories.Values.Select(it => it.TaskName).ToArray(), cancellationToken);

            logger.LogInformation($"Subscribed executor {ExecutorId}.");
        }
        public async Task WorkAsync(CancellationToken cancellationToken)
        {
            if (isStarted)
                throw new InvalidOperationException();
            isStarted = true;

            await WaitingTasksCycleAsync(cancellationToken);

            isStarted = false;
        }

        private async Task WaitingTasksCycleAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Executor {ExecutorId} start working.");

            var countWaits = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation($"Executor {ExecutorId} waiting tasks...");

                TaskExecutionModel[] commandsToExecute;

                try
                {
                    commandsToExecute = (await taskAllocator.WaitTasksAsync(ExecutorId, cancellationToken)).ToArray();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, $"Executor {ExecutorId} error waiting tasks.");

                    await Task.Delay(5000);

                    continue;
                }

                logger.LogInformation($"Executor {ExecutorId} received tasks: {commandsToExecute.Length}");

                foreach (var taskToExecute in commandsToExecute)
                    StartJob(taskToExecute, cancellationToken);

                countWaits++;
            }

            logger.LogInformation($"Executor {ExecutorId} end working.");
        }
        private void StartJob(TaskExecutionModel executionModel, CancellationToken cancellationToken)
        {
            if (!handlerFactories.TryGetValue(executionModel.TaskModel.GetType(), out TaskHandlerMetadata handlerMetadata))
                throw new InvalidOperationException();

            var jobScope = serviceProvider.CreateScope();

            var job = new TaskJob(executionModel, handlerMetadata, jobScope, this);
            job.Run(cancellationToken);
        }
        private void DetachJob(TaskJob job)
        {
            if (!_startedJobs.TryRemove(job.TaskId, out TaskJob removed))
                throw new InvalidOperationException();
            removed.Dispose();
        }

        #region IJobExecutorContext members

        public Guid ExecutorId { get; private set; }
        Task IJobExecutorContext.OnStartedJob(TaskJob job)
        {
            if (!_startedJobs.TryAdd(job.TaskId, job))
                throw new InvalidOperationException("Не удалось добавить задачу в список выполняемых.");

            logger.LogInformation($"Task {job.TaskId} started.");

            return Task.CompletedTask;
        }
        async Task IJobExecutorContext.OnSuccessJob(TaskJob job)
        {
            DetachJob(job);

            logger.LogInformation($"Task {job.TaskId} success.");

            await taskAllocator.SuccessTaskAsync(ExecutorId, job.TaskId, job.Elapsed, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
        }
        async Task IJobExecutorContext.OnDefferJob(TaskJob job)
        {
            DetachJob(job);

            logger.LogInformation($"Task {job.TaskId} deffered.");

            await taskAllocator.DeferTaskAsync(ExecutorId, job.TaskId, CancellationToken.None);

            Interlocked.Increment(ref cancelledCommands);
        }
        async Task IJobExecutorContext.OnTimeoutJob(TaskJob job)
        {
            DetachJob(job);

            logger.LogWarning($"Task {job.TaskId} execution timeout.");

            await taskAllocator.ErrorTaskAsync(ExecutorId, job.TaskId, job.Elapsed, new TimeoutException("Timeout task executing."), CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
            Interlocked.Increment(ref faultedCommands);
        }
        async Task IJobExecutorContext.OnErrorJob(TaskJob job, Exception exception)
        {
            DetachJob(job);

            logger.LogError(exception, $"Task {job.TaskId} error.");

            await taskAllocator.ErrorTaskAsync(ExecutorId, job.TaskId, job.Elapsed, exception, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
            Interlocked.Increment(ref faultedCommands);
        }
        async Task IJobExecutorContext.OnUnhandledError(TaskJob job, Exception exception)
        {
            DetachJob(job);

            logger.LogCritical(exception, $"Task {job.TaskId} unhandled error.");

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