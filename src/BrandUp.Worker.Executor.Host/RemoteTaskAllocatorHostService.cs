using BrandUp.Worker.Allocator;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor
{
    internal class RemoteTaskAllocatorHostService : BackgroundService
    {
        private readonly ITaskAllocator taskAllocator;

        public RemoteTaskAllocatorHostService(ITaskAllocator taskAllocator)
        {
            this.taskAllocator = taskAllocator ?? throw new ArgumentNullException(nameof(taskAllocator));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await taskAllocator.StartAsync(stoppingToken);
        }
    }
}