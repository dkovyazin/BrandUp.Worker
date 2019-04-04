using BrandUp.Worker.Executor;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public class RemoteTaskAllocator : ITaskAllocator, IExecutorConnection
    {
        private readonly WorkerServiceClient workerClient;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<RemoteTaskAllocator> logger;
        private readonly Dictionary<Type, TaskHandlerMetadata> handlerFactories = new Dictionary<Type, TaskHandlerMetadata>();

        public RemoteTaskAllocator(WorkerServiceClient workerClient, ITaskMetadataManager metadataManager, ITaskHandlerManager handlerManager, IServiceProvider serviceProvider, ILogger<RemoteTaskAllocator> logger)
        {
            this.workerClient = workerClient ?? throw new ArgumentNullException(nameof(workerClient));
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            foreach (var taskType in handlerManager.TaskTypes)
            {
                var taskMetadata = metadataManager.FindTaskMetadata(taskType);
                if (taskMetadata == null)
                    throw new InvalidOperationException();

                handlerFactories.Add(taskType, new TaskHandlerMetadata(taskMetadata.TaskName, TaskExecutor.HandlerBaseType.MakeGenericType(taskType)));
            }
        }

        #region IExecutorConnection members

        public Guid ExecutorId { get; private set; }
        public bool TryGetHandlerMetadata(Type taskType, out TaskHandlerMetadata handlerFactory)
        {
            return handlerFactories.TryGetValue(taskType, out handlerFactory);
        }

        #endregion

        #region ITaskAllocator members

        public string Name => workerClient.ServiceUrl.ToString();

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Starting executor...");

            ExecutorId = await SubscribeAsync(handlerFactories.Values.Select(it => it.TaskName).ToArray(), stoppingToken);

            logger.LogInformation($"Subscribed executor {ExecutorId}.");

            var taskExecutor = serviceProvider.GetRequiredService<TaskExecutor>();
            await taskExecutor.WorkAsync(this, stoppingToken);
        }

        public Task<Guid> PushTaskAsync(object taskModel, CancellationToken cancellationToken = default)
        {
            return workerClient.PushTaskAsync(taskModel, cancellationToken);
        }

        public Task<Guid> SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken = default)
        {
            return workerClient.SubscribeAsync(taskTypeNames, cancellationToken);
        }

        public Task<IEnumerable<TaskToExecute>> WaitTasksAsync(Guid executorId, CancellationToken cancellationToken = default)
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