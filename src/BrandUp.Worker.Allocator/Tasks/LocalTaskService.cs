using BrandUp.Worker.Allocator;
using System;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    internal class LocalTaskService : ITaskService
    {
        private readonly ITaskAllocator taskAllocator;

        public LocalTaskService(ITaskAllocator taskAllocator)
        {
            this.taskAllocator = taskAllocator ?? throw new ArgumentNullException(nameof(taskAllocator));
        }

        public Task<Guid> PushTask(object taskModel)
        {
            return taskAllocator.PushTaskAsync(taskModel);
        }
    }
}