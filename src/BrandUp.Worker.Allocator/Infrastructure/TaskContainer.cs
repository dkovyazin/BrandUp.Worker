using System;

namespace BrandUp.Worker.Allocator.Infrastructure
{
    public class TaskContainer
    {
        public Guid TaskId { get; }
        public object TaskModel { get; }
        public DateTime CreatedDate { get; }

        public TaskContainer(Guid taskId, object taskModel, DateTime createdDate)
        {
            TaskId = taskId;
            TaskModel = taskModel;
            CreatedDate = createdDate;
        }

        #region Object members

        public override int GetHashCode()
        {
            return TaskId.GetHashCode();
        }

        #endregion
    }
}