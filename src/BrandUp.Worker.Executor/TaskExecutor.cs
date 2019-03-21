﻿using BrandUp.Worker.Allocator;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor
{
    public class TaskExecutor : IJobExecutorContext, IDisposable
    {
        #region Fields

        private static readonly Type HandlerBaseType = typeof(TaskHandler<>);
        private readonly ITaskMetadataManager metadataManager;
        private readonly ITaskAllocator taskAllocator;
        private readonly ITaskHandlerManager handlerManager;
        private readonly IServiceProvider serviceProvider;
        private bool isStarted = false;
        private int executedCommands = 0;
        private int faultedCommands = 0;
        private int cancelledCommands = 0;
        private readonly Dictionary<Type, TaskHandlerFactory> handlerFactories = new Dictionary<Type, TaskHandlerFactory>();
        private readonly ConcurrentDictionary<Guid, JobTask> _startedJobs = new ConcurrentDictionary<Guid, JobTask>();

        #endregion

        #region Properties

        public bool IsStarted => isStarted;
        public int ExecutingCommands => _startedJobs.Count;
        public int ExecutedCommands => executedCommands;
        public int FaultedCommands => faultedCommands;
        public int CancelledCommands => cancelledCommands;

        #endregion

        public TaskExecutor(ITaskMetadataManager metadataManager, ITaskHandlerManager handlerManager, ITaskAllocator taskAllocator, IServiceProvider serviceProvider)
        {
            this.metadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));
            this.handlerManager = handlerManager ?? throw new ArgumentNullException(nameof(handlerManager));
            this.taskAllocator = taskAllocator ?? throw new ArgumentNullException(nameof(taskAllocator));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task WorkAsync(CancellationToken cancellationToken)
        {
            if (isStarted)
                throw new InvalidOperationException();
            isStarted = true;

            foreach (var taskType in handlerManager.TaskTypes)
            {
                var taskMetadata = metadataManager.FindTaskMetadata(taskType);
                if (taskMetadata == null)
                    throw new InvalidOperationException();

                handlerFactories.Add(taskType, new TaskHandlerFactory(taskMetadata.TaskName, HandlerBaseType.MakeGenericType(taskType)));
            }

            ExecutorId = await taskAllocator.SubscribeAsync(handlerFactories.Values.Select(it => it.TaskName).ToArray(), cancellationToken);

            await WaitingCommandsCycleAsync(cancellationToken);

            isStarted = false;
        }

        private async Task WaitingCommandsCycleAsync(CancellationToken cancellationToken)
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
            if (!handlerFactories.TryGetValue(taskToExecute.Task.GetType(), out TaskHandlerFactory handlerFactory))
                throw new InvalidOperationException();

            var jobScope = serviceProvider.CreateScope();
            try
            {
                var taskHandler = (ITaskHandler)jobScope.ServiceProvider.GetRequiredService(handlerFactory.HandlerType);

                var job = new JobTask(taskToExecute.TaskId, taskToExecute.Task, taskHandler, this);
                if (!_startedJobs.TryAdd(job.TaskId, job))
                    throw new InvalidOperationException();

                job.Start(cancellationToken, taskToExecute.Timeout);
            }
            catch (Exception ex)
            {
                jobScope.Dispose();

                throw ex;
            }
        }

        public void Dispose()
        {
        }

        #region IJobExecutorContext members

        public Guid ExecutorId { get; private set; }
        async Task IJobExecutorContext.OnSuccessJob(JobTask job)
        {
            if (!_startedJobs.TryRemove(job.TaskId, out JobTask removed))
                throw new InvalidOperationException();

            await taskAllocator.SuccessTaskAsync(ExecutorId, job.TaskId, job.Elapsed, CancellationToken.None);

            Interlocked.Increment(ref executedCommands);
        }
        async Task IJobExecutorContext.OnCancelledJob(JobTask job)
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

        private class TaskHandlerFactory
        {
            public string TaskName { get; }
            public Type HandlerType { get; }

            public TaskHandlerFactory(string taskName, Type handlerType)
            {
                TaskName = taskName;
                HandlerType = handlerType;
            }
        }
    }
}