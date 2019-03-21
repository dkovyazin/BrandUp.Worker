﻿using BrandUp.Worker.Allocator.Infrastructure;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocator : ITaskAllocator, IDisposable
    {
        #region Fields

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly TaskQueue commandQueue;
        private ConcurrentDictionary<Guid, TaskExecutor> executors = new ConcurrentDictionary<Guid, TaskExecutor>();
        private ConcurrentDictionary<Guid, CommandWaiting> executorWaitings = new ConcurrentDictionary<Guid, CommandWaiting>();
        private readonly TimeSpan timeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(30);
        private readonly int maxTasksPerExecutor = 10;
        private int countExecutingTasks = 0;
        private readonly ITaskRepository taskRepository;

        #endregion

        #region Properties

        public ITaskMetadataManager MetadataManager { get; }
        public TimeSpan DefaultCommandWaitingTimeout => timeoutWaitingTasksPerExecutor;
        public int CountExecutors => executors.Count;
        public int CountExecutorWaitings => executorWaitings.Count;
        public int CountCommandExecuting => countExecutingTasks;
        public int CountCommandInQueue => commandQueue.Count;

        #endregion

        public TaskAllocator(ITaskMetadataManager metadataManager, ITaskRepository taskRepository, IOptions<TaskAllocatorOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var optionsValue = options.Value;

            if (optionsValue.TimeoutWaitingTasksPerExecutor > TimeSpan.Zero)
                timeoutWaitingTasksPerExecutor = optionsValue.TimeoutWaitingTasksPerExecutor;
            if (optionsValue.MaxTasksPerExecutor > 0)
                maxTasksPerExecutor = optionsValue.MaxTasksPerExecutor;

            MetadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));
            this.taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            commandQueue = new TaskQueue(metadataManager.Tasks.Select(it => it.TaskType).ToArray());
        }

        #region Methods

        public async Task ResporeTasksAsync()
        {
            foreach (var taskState in await taskRepository.GetActualTasks())
            {
                var taskMetadata = MetadataManager.FindTaskMetadata(taskState.TaskModel);
                if (taskMetadata == null)
                    throw new ArgumentException();

                var taskContainer = new TaskContainer(taskState.TaskId, taskState.TaskModel, taskState.CreatedDate);

                if (taskState.ExecutorId.HasValue)
                {
                    if (!executors.TryGetValue(taskState.ExecutorId.Value, out TaskExecutor executor))
                        executors.TryAdd(taskState.ExecutorId.Value, executor = new TaskExecutor(taskState.ExecutorId.Value));

                    executor.AddTask(taskContainer);
                    Interlocked.Increment(ref countExecutingTasks);
                }
                else
                    commandQueue.Enqueue(taskContainer);
            }
        }

        public Guid PushTask(object taskModel, out bool isStarted, out Guid executorId)
        {
            if (taskModel == null)
                throw new ArgumentNullException(nameof(taskModel));

            var taskMetadata = MetadataManager.FindTaskMetadata(taskModel);
            if (taskMetadata == null)
                throw new ArgumentException($"Тип модели задачи {taskModel.GetType().AssemblyQualifiedName} не зарегистрирован.", nameof(taskModel));

            var taskId = Guid.NewGuid();
            var taskContainer = new TaskContainer(taskId, taskModel, DateTime.UtcNow);

            taskRepository.PushTaskAsync(taskId, taskModel, DateTime.UtcNow);

            isStarted = TryCommandExecute(taskContainer, out TaskExecutor executor);
            if (isStarted)
            {
                executorId = executor.ExecutorId;
                Interlocked.Increment(ref countExecutingTasks);
            }
            else
            {
                executorId = Guid.Empty;
                commandQueue.Enqueue(taskContainer);
            }

            return taskId;
        }
        private bool TryCommandExecute(TaskContainer taskContainer, out TaskExecutor executor)
        {
            var taskType = taskContainer.TaskModel.GetType();

            foreach (var workerWaiting in executorWaitings.Values)
            {
                if (!workerWaiting.Executor.IsSupportCommandType(taskType))
                    continue;

                if (!executorWaitings.TryRemove(workerWaiting.Executor.ExecutorId, out CommandWaiting waiting))
                    continue;

                taskRepository.TaskStartedAsync(taskContainer.TaskId, waiting.Executor.ExecutorId, DateTime.UtcNow);

                if (waiting.SendTask(taskContainer))
                {
                    executor = waiting.Executor;
                    return true;
                }
                else
                    taskRepository.TaskDeferedAsync(taskContainer.TaskId);

                waiting.Dispose();
            }

            executor = null;
            return false;
        }

        public ConnectExecutorResult ConnectExecutor(ExecutorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var commandTypes = new List<Type>();
            foreach (var commandTypeName in options.CommandTypeNames)
            {
                var commandMetadata = MetadataManager.FindTaskMetadata(commandTypeName);
                if (commandMetadata == null)
                    return ConnectExecutorResult.ErrorResult("Не найден один из типов команды.");

                commandTypes.Add(commandMetadata.TaskType);
            }

            if (commandTypes.Count == 0)
                return ConnectExecutorResult.ErrorResult("Не указано ни одного типа команды.");

            var handlerId = Guid.NewGuid();
            var handler = new TaskExecutor(handlerId, commandTypes);

            if (!executors.TryAdd(handlerId, handler))
                return ConnectExecutorResult.ErrorResult("Не удалось зафиксировать подключение воркера.");

            return ConnectExecutorResult.SuccessResult(handlerId);
        }

        private async Task<List<TaskContainer>> FindTasksToExecuteAsync(TaskExecutor executor)
        {
            var commandsToExecute = new List<TaskContainer>();
            var utcNow = DateTime.UtcNow;

            foreach (var commandType in executor.SupportedCommandTypes)
            {
                while (commandQueue.TryDequeue(commandType, out TaskContainer task))
                {
                    if (await TryTaskWaitingTimeout(task, utcNow))
                        continue;

                    commandsToExecute.Add(task);

                    if (commandsToExecute.Count >= maxTasksPerExecutor)
                        break;
                }

                if (commandsToExecute.Count >= maxTasksPerExecutor)
                    break;
            }

            return commandsToExecute;
        }
        private async Task<bool> TryTaskWaitingTimeout(TaskContainer task, DateTime utcNow)
        {
            var taskMetadata = MetadataManager.FindTaskMetadata(task.TaskModel);
            var timeoutWaiting = taskMetadata.TimeoutWaitingToStartInMiliseconds;
            if (timeoutWaiting > 0 && utcNow >= task.CreatedDate.AddMilliseconds(timeoutWaiting))
            {
                try
                {
                    await taskRepository.TaskCancelledAsync(task.TaskId, "Timeout waiting to start.");
                    return true;
                }
                catch
                {
                    commandQueue.Enqueue(task);
                    return false;
                }
            }

            return false;
        }
        private CommandWaiting CreateTaskWaiting(TaskExecutor executor, CancellationToken cancelationToken)
        {
            var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancelationToken, cancellation.Token);
            timeoutCancellation.CancelAfter(timeoutWaitingTasksPerExecutor);

            var commandWaiting = new CommandWaiting(executor, timeoutCancellation);
            if (!executorWaitings.TryAdd(executor.ExecutorId, commandWaiting))
                throw new InvalidOperationException("Ожидание задач для исполнителя уже зарегистрировано.");

            timeoutCancellation.Token.Register(() =>
            {
                if (executorWaitings.TryRemove(executor.ExecutorId, out CommandWaiting removed))
                {
                    removed.End();
                    removed.Dispose();
                }
            });

            return commandWaiting;
        }

        #endregion

        #region ITaskAllocator members

        Task<Guid> ITaskAllocator.PushTaskAsync(object taskModel, CancellationToken cancellationToken)
        {
            var taskId = PushTask(taskModel, out bool isStarted, out Guid executorId);
            return Task.FromResult(taskId);
        }
        Task<Guid> ITaskAllocator.SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken)
        {
            var result = ConnectExecutor(new ExecutorOptions(taskTypeNames));
            if (!result.Success)
                throw new InvalidOperationException(result.Error);
            return Task.FromResult(result.ExecutorId);
        }
        public async Task<IEnumerable<TaskToExecute>> WaitTasksAsync(Guid executorId, CancellationToken cancellationToken)
        {
            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
                throw new ArgumentException();

            cancellationToken.ThrowIfCancellationRequested();

            var tasks = await FindTasksToExecuteAsync(executor);
            if (tasks.Count == 0)
            {
                var taskWaiting = CreateTaskWaiting(executor, cancellationToken);
                tasks = await taskWaiting.WaitTask;
            }

            var utcNow = DateTime.UtcNow;
            var tasksToExecute = new List<TaskToExecute>();
            foreach (var task in tasks)
            {
                var taskMetadata = MetadataManager.FindTaskMetadata(task.TaskModel);

                await taskRepository.TaskStartedAsync(task.TaskId, executorId, utcNow);

                executor.AddTask(task);
                Interlocked.Increment(ref countExecutingTasks);

                tasksToExecute.Add(new TaskToExecute { TaskId = task.TaskId, Task = task.TaskModel, Timeout = taskMetadata.ExecutionTimeout });
            }
            return tasksToExecute;
        }
        public async Task SuccessTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, CancellationToken cancellationToken)
        {
            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
                throw new ArgumentException();

            if (!executor.TryRemoveTask(taskId, out TaskContainer command))
                throw new ArgumentException();

            Interlocked.Decrement(ref countExecutingTasks);

            await taskRepository.TaskDoneAsync(taskId, executingTime, DateTime.UtcNow);
        }
        public async Task ErrorTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, Exception exception, CancellationToken cancellationToken)
        {
            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
                throw new ArgumentException();

            if (!executor.TryRemoveTask(taskId, out TaskContainer command))
                throw new ArgumentException();

            Interlocked.Decrement(ref countExecutingTasks);

            await taskRepository.TaskErrorAsync(taskId, executingTime, DateTime.UtcNow);
        }
        public async Task DeferTaskAsync(Guid executorId, Guid taskId, CancellationToken cancellationToken)
        {
            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
                throw new ArgumentException();

            if (!executor.TryRemoveTask(taskId, out TaskContainer command))
                throw new ArgumentException();

            Interlocked.Decrement(ref countExecutingTasks);

            commandQueue.Enqueue(command);

            await taskRepository.TaskDeferedAsync(taskId);
        }

        #endregion

        #region IDisposable members

        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    cancellation.Cancel();
                    cancellation.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        private class CommandWaiting : IDisposable
        {
            private CancellationTokenSource timeoutCancellation;
            private TaskCompletionSource<List<TaskContainer>> completionSource;

            public TaskExecutor Executor { get; private set; }
            public Task<List<TaskContainer>> WaitTask => completionSource.Task;

            public CommandWaiting(TaskExecutor executor, CancellationTokenSource timeoutCancellation)
            {
                Executor = executor;
                this.timeoutCancellation = timeoutCancellation;
                completionSource = new TaskCompletionSource<List<TaskContainer>>();
            }

            public bool SendTask(TaskContainer command)
            {
                return completionSource.TrySetResult(new List<TaskContainer> { command });
            }

            public void End()
            {
                completionSource.TrySetResult(new List<TaskContainer>());
            }

            public void Dispose()
            {
                timeoutCancellation.Dispose();
            }

            #region Object members

            public override int GetHashCode()
            {
                return Executor.GetHashCode();
            }

            #endregion
        }
    }
}