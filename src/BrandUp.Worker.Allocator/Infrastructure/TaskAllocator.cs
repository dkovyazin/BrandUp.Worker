using BrandUp.Worker.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrandUp.Worker.Allocator.Infrastructure
{
    public class TaskAllocator : IDisposable
    {
        #region Fields

        private readonly ITaskMetadataManager metadataManager;
        private readonly CancellationTokenSource cancellation;
        private readonly TaskQueue commandQueue;
        private ConcurrentDictionary<Guid, TaskExecutor> executors = new ConcurrentDictionary<Guid, TaskExecutor>();
        private ConcurrentDictionary<Guid, CommandWaiting> executorWaitings = new ConcurrentDictionary<Guid, CommandWaiting>();
        private readonly TimeSpan defaultCommandWaitingTimeout = TimeSpan.FromSeconds(30);
        private readonly int maxCommandsPerExecutor = 10;
        private int countExecutingCommands = 0;

        #endregion

        #region Properties

        public ITaskMetadataManager MetadataManager => metadataManager;
        public TimeSpan DefaultCommandWaitingTimeout => defaultCommandWaitingTimeout;
        public int CountExecutors => executors.Count;
        public int CountExecutorWaitings => executorWaitings.Count;
        public int CountCommandExecuting => countExecutingCommands;
        public int CountCommandInQueue => commandQueue.Count;

        #endregion

        public TaskAllocator(CommandAllocatorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.CommandMetadataManager == null)
                throw new ArgumentNullException(nameof(options.CommandMetadataManager));

            if (options.DefaultCommandWaitingTimeout > TimeSpan.Zero)
                defaultCommandWaitingTimeout = options.DefaultCommandWaitingTimeout;
            if (options.MaxCommandsPerExecutor > 0)
                maxCommandsPerExecutor = options.MaxCommandsPerExecutor;

            metadataManager = options.CommandMetadataManager ?? throw new ArgumentNullException(nameof(options.CommandMetadataManager));
            commandQueue = new TaskQueue(metadataManager.Tasks.Select(it => it.TaskType).ToArray());
            cancellation = new CancellationTokenSource();
        }

        #region Methods

        public void PushCommand(Guid commandId, object command, out bool isStarted, out Guid executorId)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var commandMetadata = metadataManager.FindTaskMetadata(command);
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

                if (workerWaiting.TryAddCommandToExecuting(command))
                {
                    executor = workerWaiting.Executor;
                    return true;
                }
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
                var commandMetadata = metadataManager.FindTaskMetadata(commandTypeName);
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
        public WaitCommandsResult WaitCommands(Guid executorId)
        {
            return WaitCommands(executorId, CancellationToken.None);
        }
        public WaitCommandsResult WaitCommands(Guid executorId, CancellationToken cancelationToken)
        {
            return WaitCommands(executorId, cancelationToken, defaultCommandWaitingTimeout);
        }
        public WaitCommandsResult WaitCommands(Guid executorId, TimeSpan timeout)
        {
            return WaitCommands(executorId, CancellationToken.None, timeout);
        }
        public WaitCommandsResult WaitCommands(Guid executorId, CancellationToken cancelationToken, TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Таймаут не можут быть 0.", nameof(timeout));

            if (!executors.TryGetValue(executorId, out TaskExecutor executor))
                return WaitCommandsResult.ErrorResult("Не найдено подключение воркера.");

            if (cancelationToken.IsCancellationRequested)
                return WaitCommandsResult.ErrorResult("Ожидание комманд на выполнение было отменено до завершения регистрации в очереди ожидающих.");

            var commandsToExecute = FindCommandsToExecute(executor);
            if (commandsToExecute.Count == 0)
            {
                var commandWaiting = AddCommandWaiting(executor, cancelationToken, timeout);
                if (!commandWaiting.WaitCommands(out commandsToExecute))
                    return WaitCommandsResult.SuccessEmpty();
            }

            foreach (var command in commandsToExecute)
            {
                executor.AddCommand(command);

                Interlocked.Increment(ref countExecutingCommands);
            }

            return WaitCommandsResult.SuccessResult(commandsToExecute.Select(it => new CommandToExecute(it.TaskId, it.Task)).ToList());
        }

        private List<TaskContainer> FindCommandsToExecute(TaskExecutor executor)
        {
            var commandsToExecute = new List<TaskContainer>();
            foreach (var commandType in executor.SupportedCommandTypes)
            {
                while (commandQueue.TryDequeue(commandType, out Guid commandId, out object command))
                {
                    commandsToExecute.Add(new TaskContainer(commandId, command));

                    if (commandsToExecute.Count >= maxCommandsPerExecutor)
                        break;
                }

                if (commandsToExecute.Count >= maxCommandsPerExecutor)
                    break;
            }

            return commandsToExecute;
        }

        private CommandWaiting AddCommandWaiting(TaskExecutor executor, CancellationToken cancelationToken, TimeSpan timeout)
        {
            if (cancelationToken.IsCancellationRequested)
                throw new ArgumentException();

            var waitingCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancelationToken, cancellation.Token);
            waitingCancellation.CancelAfter(timeout);

            var commandWaiting = new CommandWaiting(executor, waitingCancellation);
            if (!executorWaitings.TryAdd(executor.ExecutorId, commandWaiting))
                throw new InvalidOperationException("Не удалось зарегистрировать ожидание комманд на выполнение.");

            waitingCancellation.Token.Register(() =>
            {
                executorWaitings.TryRemove(executor.ExecutorId, out CommandWaiting removed);
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
            private CancellationTokenSource _cancellation;
            private List<TaskContainer> _commandsToExecute = new List<TaskContainer>();
            private readonly object _sync = new object();

            public TaskExecutor Executor { get; private set; }

            public CommandWaiting(TaskExecutor executor, CancellationTokenSource cancelationTokenSource)
            {
                Executor = executor;
                _cancellation = cancelationTokenSource;
            }

            public bool WaitCommands(out List<TaskContainer> commandsToExecute)
            {
                if (!_cancellation.Token.WaitHandle.WaitOne())
                    throw new InvalidOperationException();

                lock (_sync)
                {
                    if (_commandsToExecute.Count > 0)
                    {

                        commandsToExecute = _commandsToExecute.ToList();
                        _commandsToExecute = null;

                        return true;
                    }
                    else
                    {
                        commandsToExecute = null;
                        return false;
                    }
                }
            }
            public bool TryAddCommandToExecuting(TaskContainer command)
            {
                lock (_sync)
                {
                    if (_cancellation.IsCancellationRequested)
                        return false;

                    _commandsToExecute.Add(command);

                    _cancellation.Cancel();

                    return true;
                }
            }

            public void Dispose()
            {
                _cancellation.Dispose();
            }

            #region Object members

            public override int GetHashCode()
            {
                return Executor.GetHashCode();
            }

            #endregion
        }
    }

    public class CommandAllocatorOptions
    {
        public ITaskMetadataManager CommandMetadataManager { get; set; }
        public TimeSpan DefaultCommandWaitingTimeout { get; set; }
        public int MaxCommandsPerExecutor { get; set; }

        public CommandAllocatorOptions() { }
        public CommandAllocatorOptions(ITaskMetadataManager commandMetadataManager)
        {
            CommandMetadataManager = commandMetadataManager ?? throw new ArgumentNullException(nameof(commandMetadataManager));
        }
    }
}