using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public interface ITaskAllocator
    {
        Task StartAsync(CancellationToken stoppingToken);
        Task<Guid> PushTaskAsync(object taskModel, CancellationToken cancellationToken = default);
        Task<Guid> SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken = default);
        Task<IEnumerable<TaskToExecute>> WaitTasksAsync(Guid executorId, CancellationToken cancellationToken = default);
        Task SuccessTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, CancellationToken cancellationToken = default);
        Task ErrorTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, Exception exception, CancellationToken cancellationToken = default);
        Task DeferTaskAsync(Guid executorId, Guid taskId, CancellationToken cancellationToken = default);
    }

    public class TaskToExecute
    {
        public Guid TaskId { get; set; }
        public object Task { get; set; }
        public int Timeout { get; set; }
    }
}