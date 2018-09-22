using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BrandUp.Worker.Allocator
{
    internal class TaskExecutor
    {
        private readonly HashSet<Type> _supportedCommandTypes;
        private readonly ConcurrentDictionary<Guid, TaskContainer> _executingCommands = new ConcurrentDictionary<Guid, TaskContainer>();

        public readonly Guid ExecutorId;
        public IEnumerable<Type> SupportedCommandTypes => _supportedCommandTypes;
        public int CountExecutingCommands => _executingCommands.Count;

        public TaskExecutor(Guid id, List<Type> supportedCommandTypes)
        {
            if (supportedCommandTypes == null || supportedCommandTypes.Count == 0)
                throw new ArgumentException("Список доступных очередей команд не может быть пустым.", nameof(supportedCommandTypes));

            ExecutorId = id;
            _supportedCommandTypes = new HashSet<Type>(supportedCommandTypes);
        }

        public bool IsSupportCommandType(Type type)
        {
            return _supportedCommandTypes.Contains(type);
        }
        public void AddCommand(TaskContainer command)
        {
            if (!_executingCommands.TryAdd(command.TaskId, command))
                throw new InvalidOperationException();
        }
        public bool TryRemoveCommand(Guid commandId, out TaskContainer command)
        {
            return _executingCommands.TryRemove(commandId, out command);
        }

        #region Object members

        public override int GetHashCode()
        {
            return ExecutorId.GetHashCode();
        }

        #endregion
    }
}
