using BrandUp.Worker.Executor;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public class RemoteTaskAllocator : ITaskAllocator
    {
        private readonly WorkerServiceClient workerClient;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<RemoteTaskAllocator> logger;

        public RemoteTaskAllocator(WorkerServiceClient workerClient, ITaskMetadataManager metadataManager, IServiceProvider serviceProvider, ILogger<RemoteTaskAllocator> logger)
        {
            this.workerClient = workerClient ?? throw new ArgumentNullException(nameof(workerClient));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region ITaskAllocator members

        public string Name => workerClient.ServiceUrl.ToString();

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            var taskExecutor = serviceProvider.GetRequiredService<TaskExecutor>();

            await taskExecutor.ConnectAsync(stoppingToken);

            await taskExecutor.WorkAsync(stoppingToken);
        }

        public Task<Guid> PushTaskAsync(object taskModel, CancellationToken cancellationToken = default)
        {
            return workerClient.PushTaskAsync(taskModel, cancellationToken);
        }

        public Task<Guid> SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken = default)
        {
            return workerClient.SubscribeAsync(taskTypeNames, cancellationToken);
        }

        public Task<IEnumerable<TaskExecutionModel>> WaitTasksAsync(Guid executorId, CancellationToken cancellationToken = default)
        {
            return workerClient.WaitTasksAsync(executorId, cancellationToken);
        }

        public Task SuccessTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, CancellationToken cancellationToken = default)
        {
            return workerClient.SuccessTaskAsync(executorId, taskId, executingTime, cancellationToken);
        }

        public Task ErrorTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, Exception exception, CancellationToken cancellationToken = default)
        {
            return workerClient.ErrorTaskAsync(executorId, taskId, executingTime, exception, cancellationToken);
        }

        public Task DeferTaskAsync(Guid executorId, Guid taskId, CancellationToken cancellationToken = default)
        {
            return workerClient.DeferTaskAsync(executorId, taskId, cancellationToken);
        }

        #endregion
    }
}