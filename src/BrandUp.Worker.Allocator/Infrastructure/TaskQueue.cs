using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BrandUp.Worker.Allocator.Infrastructure
{
    internal class TaskQueue
    {
        private Dictionary<Type, TaskTypeQueue> queues = new Dictionary<Type, TaskTypeQueue>();
        private volatile int count = 0;

        public IEnumerable<Type> TaskTypes => queues.Keys;
        public int CountTaskTypes => queues.Count;
        public int Count => count;

        public TaskQueue(Type[] taskTypes)
        {
            if (taskTypes == null)
                throw new ArgumentNullException(nameof(taskTypes));

            InitializeQueues(taskTypes);
        }

        private void InitializeQueues(Type[] taskTypes)
        {
            foreach (var commandType in taskTypes)
            {
                queues.Add(commandType, new TaskTypeQueue(commandType));
            }
        }

        public bool HasTaskType(Type taskType)
        {
            if (taskType == null)
                throw new ArgumentNullException(nameof(taskType));
            return queues.ContainsKey(taskType);
        }
        public void Enqueue(TaskContainer task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (!queues.TryGetValue(task.Task.GetType(), out TaskTypeQueue taskQueue))
                throw new ArgumentException("Тип команды не поддерживается.", nameof(task));

            taskQueue.Enqueue(task);

            System.Threading.Interlocked.Increment(ref count);
        }
        public bool TryDequeue(Type taskType, out Guid taskId, out object task)
        {
            if (taskType == null)
                throw new ArgumentNullException(nameof(taskType));

            if (!queues.TryGetValue(taskType, out TaskTypeQueue commandQueue))
                throw new ArgumentException("Тип задачи не поддерживается.", nameof(taskType));

            if (!commandQueue.TryDequeue(out TaskContainer commandContainer))
            {
                taskId = Guid.Empty;
                task = null;
                return false;
            }

            System.Threading.Interlocked.Decrement(ref count);

            taskId = commandContainer.TaskId;
            task = commandContainer.Task;
            return true;
        }

        private class TaskTypeQueue
        {
            private ConcurrentQueue<TaskContainer> queue = new ConcurrentQueue<TaskContainer>();

            public readonly Type TaskType;
            public bool HasCommands => queue.Count > 0;
            public int CountCommands => queue.Count;

            public TaskTypeQueue(Type taskType)
            {
                TaskType = taskType;
            }

            public void Enqueue(TaskContainer taskContainer)
            {
                queue.Enqueue(taskContainer);
            }
            public bool TryDequeue(out TaskContainer taskContainer)
            {
                return queue.TryDequeue(out taskContainer);
            }

            #region Object members

            public override int GetHashCode()
            {
                return TaskType.GetHashCode();
            }

            #endregion
        }
    }
}