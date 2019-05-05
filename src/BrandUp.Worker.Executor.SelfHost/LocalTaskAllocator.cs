using BrandUp.Worker.Executor;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public class LocalTaskAllocator : TaskAllocator
    {
        private readonly IServiceProvider serviceProvider;

        public LocalTaskAllocator(ITaskMetadataManager metadataManager, ITaskRepository taskRepository, IOptions<TaskAllocatorOptions> options, IServiceProvider serviceProvider, ILogger<TaskAllocator> logger) : base(metadataManager, taskRepository, options, logger)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected override async Task OnStartAsync(CancellationToken stoppingToken)
        {
            await base.OnStartAsync(stoppingToken);

            var taskExecutor = serviceProvider.GetRequiredService<TaskExecutor>();

            await taskExecutor.ConnectAsync(stoppingToken);

            await taskExecutor.WorkAsync(stoppingToken);
        }
    }
}