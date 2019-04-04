using BrandUp.Worker.Executor;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public class LocalTaskAllocator : TaskAllocator, IExecutorConnection
    {
        private readonly Dictionary<Type, TaskHandlerMetadata> handlerFactories = new Dictionary<Type, TaskHandlerMetadata>();
        private readonly IServiceProvider serviceProvider;

        public LocalTaskAllocator(ITaskMetadataManager metadataManager, ITaskRepository taskRepository, IOptions<TaskAllocatorOptions> options, ITaskHandlerManager handlerManager, IServiceProvider serviceProvider, ILogger<TaskAllocator> logger) : base(metadataManager, taskRepository, options, logger)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

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

        protected override async Task OnStartAsync(CancellationToken stoppingToken)
        {
            await base.OnStartAsync(stoppingToken);

            ExecutorId = await SubscribeAsync(handlerFactories.Values.Select(it => it.TaskName).ToArray(), stoppingToken);

            var taskExecutor = serviceProvider.GetRequiredService<TaskExecutor>();
            await taskExecutor.WorkAsync(this, stoppingToken);
        }
    }
}