using System;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    public interface ITaskRepository
    {
        Task PushTask(Guid taskId, object taskModel, DateTime createdDate);
        Task TaskStarted(Guid taskId, Guid executorId, DateTime startedDate);
        Task TaskDefered(Guid taskId);
        Task TaskDone(Guid taskId, TimeSpan executingTime, DateTime doneDate);
    }

    public class DefaultTaskRepository : ITaskRepository
    {
        public Task PushTask(Guid taskId, object taskModel, DateTime createdDate)
        {
            return Task.CompletedTask;
        }

        public Task TaskDefered(Guid taskId)
        {
            return Task.CompletedTask;
        }

        public Task TaskDone(Guid taskId, TimeSpan executingTime, DateTime doneDate)
        {
            return Task.CompletedTask;
        }

        public Task TaskStarted(Guid taskId, Guid executorId, DateTime startedDate)
        {
            return Task.CompletedTask;
        }
    }
}