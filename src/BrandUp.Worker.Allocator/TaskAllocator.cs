using BrandUp.Worker.Tasks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator.Infrastructure
{
    public class TaskAllocator : ITaskAllocator, IDisposable
    {
        #region Fields

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly TaskQueue commandQueue;
        private ConcurrentDictionary<Guid, TaskExecutor> executors = new ConcurrentDictionary<Guid, TaskExecutor>();
        private ConcurrentDictionary<Guid, CommandWaiting> executorWaitings = new ConcurrentDictionary<Guid, CommandWaiting>();
        private readonly TimeSpan defaultCommandWaitingTimeout = TimeSpan.FromSeconds(30);
        private readonly int maxCommandsPerExecutor = 10;
        private int countExecutingCommands = 0;

        #endregion

        #region Properties

        public ITaskMetadataManager MetadataManager { get; }
        public TimeSpan DefaultCommandWaitingTimeout => defaultCommandWaitingTimeout;
        public int CountExecutors => executors.Count;
        public int CountExecutorWaitings => executorWaitings.Count;
        public int CountCommandExecuting => countExecutingCommands;
        public int CountCommandInQueue => commandQueue.Count;

        #endregion

        public TaskAllocator(ITaskMetadataManager metadataManager, IOptions<TaskAllocatorOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var optionsValue = options.Value;

            if (optionsValue.DefaultTaskWaitingTimeout > TimeSpan.Zero)
                defaultCommandWaitingTimeout = optionsValue.DefaultTaskWaitingTimeout;
            if (optionsValue.MaxTasksPerExecutor > 0)
                maxCommandsPerExecutor = optionsValue.MaxTasksPerExecutor;

            MetadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));
            commandQueue = new TaskQueue(metadataManager.Tasks.Select(it => it.TaskType).ToArray());
        }

        #region Methods

        public void PushCommand(Guid commandId, object command, out bool isStarted, out Guid executorId)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var commandMetadata = MetadataManager.FindTaskMetadata(command);
            if (commandMetadata == null)
                throw new ArgumentException();

            var commandContainer = new TaskContainer(commandId, command);

            isStarted = TryCommandExecute(commandContainer, out TaskExecutor executor);
            if (isStarted)
                executorId = executor.ExecutorId;
            else
            {
                executorId = Guid.Empty;
                commandQueue.Enqueue(commandContainer);
            }
        }

        private bool TryCommandExecute(TaskContainer command, out TaskExecutor executor)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var commandType = command.Task.GetType();

            foreach (var workerWaiting in executorWaitings.Values)
            {
                if (!workerWaiting.Executor.IsSupportCommandType(commandType))
                    continue;

                if (!executorWaitings.TryRemove(workerWaiting.Executor.ExecutorId, out CommandWaiting waiting))
                    continue;

                if (waiting.SendTask(command))
                {
                    executor = workerWaiting.Executor;
                    return true;
                }

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
        public Task<WaitCommandsResult> WaitTasks(Guid executorId)
        {
            return WaitTasks(executorId, CancellationToken.None);
        }
        public Task<WaitCommandsResult> WaitTasks(Guid executorId, CancellationToken cancelationToken)
        {
            return WaitTasksAsync(executorId, cancelationToken, defaultCommandWaitingTimeout);
        }
        public Task<WaitCommandsResult> WaitTasks(Guid executorId, TimeSpan timeout)
        {
            return WaitTasksAsync(executorId, CancellationToken.None, timeout);
        }
        public async Task<WaitCommandsResult> WaitTasksAsync(Guid executorId, CancellationToken cancelationToken, TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Таймаут не можут быть 0.", nameof(timeout));

            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
                return WaitCommandsResult.ErrorResult("Не найдено подключение воркера.");

            if (cancelationToken.IsCancellationRequested)
                return WaitCommandsResult.ErrorResult("Ожидание комманд на выполнение было отменено до завершения регистрации в очереди ожидающих.");

            var tasksToExecute = FindCommandsToExecute(executor);
            if (tasksToExecute.Count == 0)
            {
                var taskWaiting = CreateTaskWaiting(executor, cancelationToken, timeout);
                tasksToExecute = await taskWaiting.WaitTask;
            }

            foreach (var command in tasksToExecute)
            {
                executor.AddCommand(command);

                Interlocked.Increment(ref countExecutingCommands);
            }

            return WaitCommandsResult.SuccessResult(tasksToExecute.Select(it => new CommandToExecute(it.TaskId, it.Task)).ToList());
        }

        private List<TaskContainer> FindCommandsToExecute(TaskExecutor executor)
        {
            var commandsToExecute = new List<TaskContainer>();
            foreach (var commandType in executor.SupportedCommandTypes)
            {
                while (commandQueue.TryDequeue(commandType, out TaskContainer task))
                {
                    commandsToExecute.Add(task);

                    if (commandsToExecute.Count >= maxCommandsPerExecutor)
                        break;
                }

                if (commandsToExecute.Count >= maxCommandsPerExecutor)
                    break;
            }

            return commandsToExecute;
        }

        private CommandWaiting CreateTaskWaiting(TaskExecutor executor, CancellationToken cancelationToken, TimeSpan timeout)
        {
            if (cancelationToken.IsCancellationRequested)
                throw new ArgumentException();

            var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancelationToken, cancellation.Token);
            timeoutCancellation.CancelAfter(timeout);

            var commandWaiting = new CommandWaiting(executor, timeoutCancellation);
            if (!executorWaitings.TryAdd(executor.ExecutorId, commandWaiting))
                throw new InvalidOperationException("Ожидание задач для исполнителя уже зарегистрировано.");

            timeoutCancellation.Token.Register(() =>
            {
                executorWaitings.TryRemove(executor.ExecutorId, out CommandWaiting removed);
                removed.End();
                removed.Dispose();
            });

            return commandWaiting;
        }
        public bool DoneCommandExecuting(Guid executorId, Guid commandId)
        {
            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
                return false;

            if (!executor.TryRemoveCommand(commandId, out TaskContainer command))
                return false;

            Interlocked.Decrement(ref countExecutingCommands);

            return true;
        }
        public bool ReturnCommandToQueue(Guid executorId, Guid commandId)
        {
            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
                return false;

            if (!executor.TryRemoveCommand(commandId, out TaskContainer command))
                return false;

            Interlocked.Decrement(ref countExecutingCommands);

            commandQueue.Enqueue(command);

            return true;
        }
        public bool TryPullExecutingCommand(Guid executorId, Guid commandId, out object command)
        {
            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
            {
                command = null;
                return false;
            }

            if (!executor.TryRemoveCommand(commandId, out TaskContainer commandContainer))
            {
                command = null;
                return false;
            }

            Interlocked.Decrement(ref countExecutingCommands);

            command = commandContainer.Task;
            return true;
        }

        #endregion

        Task<Guid> ITaskAllocator.PushTask(object taskModel)
        {
            var taskId = Guid.NewGuid();
            PushCommand(taskId, taskModel, out bool isStarted, out Guid executorId);
            return Task.FromResult(taskId);
        }

        Task<Guid> ITaskAllocator.SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken)
        {
            var result = ConnectExecutor(new ExecutorOptions(taskTypeNames));
            if (!result.Success)
                throw new InvalidOperationException(result.Error);
            return Task.FromResult(result.ExecutorId);
        }

        async Task<IEnumerable<TaskToExecute>> ITaskAllocator.WaitTasksAsync(Guid executorId, CancellationToken cancellationToken)
        {
            var result = await WaitTasks(executorId, cancellationToken);
            if (!result.Success)
                throw new InvalidOperationException(result.Error);

            return result.Commands.Select(it => new TaskToExecute { TaskId = it.CommandId, Task = it.Command });
        }

        Task ITaskAllocator.SuccessTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, CancellationToken cancellationToken)
        {
            DoneCommandExecuting(executorId, taskId);

            return Task.CompletedTask;
        }

        Task ITaskAllocator.ErrorTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, Exception exception, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

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
                if (timeoutCancellation.IsCancellationRequested)
                    return false;

                completionSource.SetResult(new List<TaskContainer> { command });

                return true;
            }

            public void End()
            {
                completionSource.SetResult(new List<TaskContainer>());
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