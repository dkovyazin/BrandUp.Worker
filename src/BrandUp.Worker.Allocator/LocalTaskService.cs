using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker
{
    internal class LocalTaskService : ITaskService
    {
        private readonly Allocator.ITaskAllocator taskAllocator;

        public LocalTaskService(Allocator.ITaskAllocator taskAllocator)
        {
            this.taskAllocator = taskAllocator ?? throw new ArgumentNullException(nameof(taskAllocator));
        }

        public Task<Guid> PushTaskAsync(object taskModel, CancellationToken cancellationToken = default)
        {
            return taskAllocator.PushTaskAsync(taskModel, cancellationToken);
        }
    }
}