using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public interface ITaskAllocator
    {
        Task<Guid> PushTask(object taskModel);
        Task<Guid> SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken);
        Task<IEnumerable<TaskToExecute>> WaitTasksAsync(Guid executorId, CancellationToken cancellationToken);
        Task SuccessTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, CancellationToken cancellationToken);
        Task ErrorTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, Exception exception, CancellationToken cancellationToken);
        Task DeferTaskAsync(Guid executorId, Guid taskId, CancellationToken cancellationToken);
    }

    public class TaskToExecute
    {
        public Guid TaskId { get; set; }
        public object Task { get; set; }
        public int Timeout { get; set; }
    }
}