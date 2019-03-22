using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator.Infrastructure
{
    internal class TaskAllocatorHostService : BackgroundService
    {
        private readonly ITaskAllocator taskAllocator;

        public TaskAllocatorHostService(ITaskAllocator taskAllocator)
        {
            this.taskAllocator = taskAllocator ?? throw new ArgumentNullException(nameof(taskAllocator));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await taskAllocator.StartAsync(stoppingToken);
        }
    }
}