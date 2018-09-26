using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    public interface ITaskRepository
    {
        Task<IEnumerable<TaskState>> GetActualTasks();
        Task PushTaskAsync(Guid taskId, object taskModel, DateTime createdDate);
        Task TaskStartedAsync(Guid taskId, Guid executorId, DateTime startedDate);
        Task TaskDeferedAsync(Guid taskId);
        Task TaskErrorAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate);
        Task TaskDoneAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate);
    }

    public class TaskState
    {
        public Guid TaskId { get; set; }
        public DateTime CreatedDate { get; set; }
        public object TaskModel { get; set; }
        public Guid? ExecutorId { get; set; }
        public DateTime? StartedDate { get; set; }
    }

    public class DefaultTaskRepository : ITaskRepository
    {
        public Task<IEnumerable<TaskState>> GetActualTasks()
        {
            return Task.FromResult<IEnumerable<TaskState>>(new List<TaskState>());
        }

        public Task PushTaskAsync(Guid taskId, object taskModel, DateTime createdDate)
        {
            return Task.CompletedTask;
        }

        public Task TaskDeferedAsync(Guid taskId)
        {
            return Task.CompletedTask;
        }

        public Task TaskDoneAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate)
        {
            return Task.CompletedTask;
        }

        public Task TaskErrorAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate)
        {
            return Task.CompletedTask;
        }

        public Task TaskStartedAsync(Guid taskId, Guid executorId, DateTime startedDate)
        {
            return Task.CompletedTask;
        }
    }
}