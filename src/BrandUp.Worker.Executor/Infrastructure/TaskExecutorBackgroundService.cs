using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor.Infrastructure
{
    internal class TaskExecutorBackgroundService : BackgroundService
    {
        private readonly TaskExecutor executor;

        public TaskExecutorBackgroundService(TaskExecutor executor)
        {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return executor.WorkAsync(stoppingToken);
        }
    }
}