using System;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    public interface ITaskRepository
    {
        Task PushTaskAsync(Guid taskId, object taskModel, DateTime createdDate);
        Task TaskStartedAsync(Guid taskId, Guid executorId, DateTime startedDate);
        Task TaskDeferedAsync(Guid taskId);
        Task TaskErrorAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate);
        Task TaskDoneAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate);
    }

    public class DefaultTaskRepository : ITaskRepository
    {
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