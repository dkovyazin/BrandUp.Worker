using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BrandUp.Worker.Allocator.Infrastructure
{
    internal class TaskExecutor
    {
        private readonly HashSet<Type> supportedTaskTypes;
        private readonly ConcurrentDictionary<Guid, TaskContainer> executingTasks = new ConcurrentDictionary<Guid, TaskContainer>();

        public readonly Guid ExecutorId;
        public IEnumerable<Type> SupportedCommandTypes => supportedTaskTypes;
        public int CountExecutingCommands => executingTasks.Count;
        public bool IsDisconnected => executingTasks == null;

        public TaskExecutor(Guid id)
        {
            ExecutorId = id;
        }
        public TaskExecutor(Guid id, List<Type> supportedTaskTypes) : this(id)
        {
            if (supportedTaskTypes == null || supportedTaskTypes.Count == 0)
                throw new ArgumentException("Список доступных очередей команд не может быть пустым.", nameof(supportedTaskTypes));

            this.supportedTaskTypes = new HashSet<Type>(supportedTaskTypes);
        }

        public bool IsSupportCommandType(Type type)
        {
            return supportedTaskTypes.Contains(type);
        }
        public void AddTask(TaskContainer command)
        {
            if (!executingTasks.TryAdd(command.TaskId, command))
                throw new InvalidOperationException();
        }
        public bool TryRemoveTask(Guid commandId, out TaskContainer command)
        {
            return executingTasks.TryRemove(commandId, out command);
        }

        #region Object members

        public override int GetHashCode()
        {
            return ExecutorId.GetHashCode();
        }

        #endregion
    }
}
