using System;

namespace BrandUp.Worker.Allocator
{
    internal class TaskContainer
    {
        public readonly Guid TaskId;
        public readonly object Task;

        public TaskContainer(Guid id, object task)
        {
            TaskId = id;
            Task = task;
        }

        #region Object members

        public override int GetHashCode()
        {
            return TaskId.GetHashCode();
        }

        #endregion
    }
}