using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public interface ITaskAllocator : ITaskService
    {
        string Name { get; }
        Task StartAsync(CancellationToken stoppingToken);
        //Task<Guid> PushTaskAsync(object taskModel, CancellationToken cancellationToken = default);
        Task<Guid> SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken = default);
        Task<IEnumerable<TaskExecutionModel>> WaitTasksAsync(Guid executorId, CancellationToken cancellationToken = default);
        Task SuccessTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, CancellationToken cancellationToken = default);
        Task ErrorTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, Exception exception, CancellationToken cancellationToken = default);
        Task DeferTaskAsync(Guid executorId, Guid taskId, CancellationToken cancellationToken = default);
    }

    public class TaskExecutionModel
    {
        public Guid TaskId { get; set; }
        public object TaskModel { get; set; }
        public int Timeout { get; set; }
    }
}