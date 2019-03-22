using BrandUp.Worker.Allocator;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor
{
    public class TaskExecutor : IJobExecutorContext, IDisposable
    {
        #region Fields

        public static readonly Type HandlerBaseType = typeof(TaskHandler<>);
        private readonly ITaskAllocator taskAllocator;
        private readonly IServiceProvider serviceProvider;
        private IExecutorConnection executorConnection;
        private bool isStarted = false;
        private int executedCommands = 0;
        private int faultedCommands = 0;
        private int cancelledCommands = 0;
        private readonly ConcurrentDictionary<Guid, JobTask> _startedJobs = new ConcurrentDictionary<Guid, JobTask>();

        #endregion

        #region Properties

        public bool IsStarted => isStarted;
        public int ExecutingCommands => _startedJobs.Count;
        public int ExecutedCommands => executedCommands;
        public int FaultedCommands => faultedCommands;
        public int CancelledCommands => cancelledCommands;

        #endregion

        public TaskExecutor(ITaskAllocator taskAllocator, IServiceProvider serviceProvider)
        {
            this.taskAllocator = taskAllocator ?? throw new ArgumentNullException(nameof(taskAllocator));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
            var countWaits = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Waiting tasks...");

                var commandsToExecute = (await taskAllocator.WaitTasksAsync(ExecutorId, cancellationToken)).ToArray();

                Console.WriteLine($"Receiver tasks: {commandsToExecute.Length}");

                foreach (var taskToExecute in commandsToExecute)
                    StartJob(taskToExecute, cancellationToken);

                countWaits++;
            }
        }

        private void StartJob(TaskToExecute taskToExecute, CancellationToken cancellationToken)
        {
            if (!executorConnection.TryGetHandlerMetadata(taskToExecute.Task.GetType(), out TaskHandlerMetadata handlerMetadata))
                throw new InvalidOperationException();

            var jobScope = serviceProvider.CreateScope();
            try
            {
                var taskHandler = (ITaskHandler)jobScope.ServiceProvider.GetRequiredService(handlerMetadata.HandlerType);

                var job = new JobTask(taskToExecute.TaskId, taskToExecute.Task, taskHandler, this);
                if (!_startedJobs.TryAdd(job.TaskId, job))
                    throw new InvalidOperationException();

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

        public void Dispose()
        {
        }

        #region IJobExecutorContext members

        public Guid ExecutorId => executorConnection.ExecutorId;
        async Task IJobExecutorContext.OnSuccessJob(JobTask job)
        {
            if (!_startedJobs.TryRemove(job.TaskId, out JobTask removed))
                throw new InvalidOperationException();

            await taskAllocator.SuccessTaskAsync(ExecutorId, job.TaskId, job.Elapsed, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
        }
        async Task IJobExecutorContext.OnDefferJob(JobTask job)
        {
            if (!_startedJobs.TryRemove(job.TaskId, out JobTask removed))
                throw new InvalidOperationException();

            await taskAllocator.DeferTaskAsync(ExecutorId, job.TaskId, CancellationToken.None);

            Interlocked.Increment(ref cancelledCommands);
        }
        async Task IJobExecutorContext.OnTimeoutJob(JobTask job)
        {
            if (!_startedJobs.TryRemove(job.TaskId, out JobTask removed))
                throw new InvalidOperationException();

            await taskAllocator.ErrorTaskAsync(ExecutorId, job.TaskId, job.Elapsed, new TimeoutException("Timeout task executing."), CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
            Interlocked.Increment(ref faultedCommands);
        }
        async Task IJobExecutorContext.OnErrorJob(JobTask job, Exception exception)
        {
            if (!_startedJobs.TryRemove(job.TaskId, out JobTask removed))
                throw new InvalidOperationException();

            await taskAllocator.ErrorTaskAsync(ExecutorId, job.TaskId, job.Elapsed, exception, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
            Interlocked.Increment(ref faultedCommands);
        }
        async Task IJobExecutorContext.OnUnhandledError(JobTask job, Exception exception)
        {
            if (!_startedJobs.TryRemove(job.TaskId, out JobTask removed))
                throw new InvalidOperationException();

            await taskAllocator.ErrorTaskAsync(ExecutorId, job.TaskId, job.Elapsed, exception, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
            Interlocked.Increment(ref faultedCommands);
        }

        #endregion
    }

    public interface IExecutorConnection
    {
        Guid ExecutorId { get; }
        bool TryGetHandlerMetadata(Type taskType, out TaskHandlerMetadata handlerFactory);
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