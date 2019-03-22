using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    public interface ITaskRepository
    {
        Task<IEnumerable<TaskState>> GetActualTasksAsync(CancellationToken cancellationToken = default);
        Task PushTaskAsync(Guid taskId, string taskTypeName, object taskModel, DateTime createdDate, CancellationToken cancellationToken = default);
        Task TaskStartedAsync(Guid taskId, Guid executorId, DateTime startedDate, CancellationToken cancellationToken = default);
        Task TaskDeferedAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task TaskErrorAsync(Guid taskId, TimeSpan executingTime, DateTime endDate, CancellationToken cancellationToken = default);
        Task TaskSuccessAsync(Guid taskId, TimeSpan executingTime, DateTime endDate, CancellationToken cancellationToken = default);
        Task TaskCancelledAsync(Guid taskId, DateTime endDate, string reason, CancellationToken cancellationToken = default);
    }

    public class TaskState
    {
        public Guid TaskId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? EndDate { get; set; }
        public object TaskModel { get; set; }
        public Guid? ExecutorId { get; set; }
        public DateTime? StartedDate { get; set; }
    }

    public class DefaultTaskRepository : ITaskRepository
    {
        public Task<IEnumerable<TaskState>> GetActualTasksAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<TaskState>>(new List<TaskState>());
        }

        public Task PushTaskAsync(Guid taskId, string taskTypeName, object taskModel, DateTime createdDate, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task TaskDeferedAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task TaskSuccessAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task TaskErrorAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task TaskStartedAsync(Guid taskId, Guid executorId, DateTime startedDate, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task TaskCancelledAsync(Guid taskId, DateTime doneDate, string reason, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}