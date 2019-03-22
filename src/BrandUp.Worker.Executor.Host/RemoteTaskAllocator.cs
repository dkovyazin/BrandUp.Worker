using BrandUp.Worker.Executor;
using BrandUp.Worker.Remoting;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public class RemoteTaskAllocator : ITaskAllocator, IExecutorConnection
    {
        private readonly WorkerServiceClient workerClient;
        private readonly IServiceProvider serviceProvider;
        private readonly Dictionary<Type, TaskHandlerMetadata> handlerFactories = new Dictionary<Type, TaskHandlerMetadata>();

        public RemoteTaskAllocator(WorkerServiceClient workerClient, ITaskMetadataManager metadataManager, ITaskHandlerManager handlerManager, IServiceProvider serviceProvider)
        {
            this.workerClient = workerClient ?? throw new ArgumentNullException(nameof(workerClient));
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

        #region ITaskAllocator members

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            ExecutorId = await SubscribeAsync(handlerFactories.Values.Select(it => it.TaskName).ToArray(), stoppingToken);

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

    public class WorkerServiceClient
    {
        private readonly HttpClient httpClient;
        private readonly IContractSerializer contractSerializer;

        public WorkerServiceClient(HttpClient httpClient, IContractSerializer contractSerializer)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.contractSerializer = contractSerializer ?? throw new ArgumentNullException(nameof(contractSerializer));
        }

        public async Task<Guid> PushTaskAsync(object taskModel, CancellationToken cancellationToken = default)
        {
            if (taskModel == null)
                throw new ArgumentNullException(nameof(taskModel));

            var requestContent = contractSerializer.CreateJsonContent(new Models.PushTaskRequest { TaskModel = taskModel });

            var response = await httpClient.PostAsync("task", requestContent, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    {
                        var responseData = await contractSerializer.DeserializeHttpResponseAsync<Models.PushTaskResponse>(response);
                        return responseData.TaskId;
                    }
                default:
                    throw new Exception();
            }
        }

        public async Task<Guid> SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken = default)
        {
            if (taskTypeNames == null)
                throw new ArgumentNullException(nameof(taskTypeNames));

            var requestContent = contractSerializer.CreateJsonContent(new Models.SubscribeExecutorRequest { TaskTypeNames = taskTypeNames });

            var response = await httpClient.PostAsync("executor/subscribe", requestContent, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    {
                        var responseData = await contractSerializer.DeserializeHttpResponseAsync<Models.SubscribeExecutorResponse>(response);
                        return responseData.ExecutorId;
                    }
                default:
                    throw new Exception();
            }
        }

        public async Task<IEnumerable<TaskToExecute>> WaitTasksAsync(Guid executorId, CancellationToken cancellationToken = default)
        {
            var requestContent = contractSerializer.CreateJsonContent(new Models.WaitTasksRequest { ExecutorId = executorId });

            var response = await httpClient.PostAsync("executor/wait", requestContent, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    {
                        var responseData = await contractSerializer.DeserializeHttpResponseAsync<Models.WaitTasksResponse>(response);
                        return responseData.Tasks;
                    }
                default:
                    throw new Exception();
            }
        }

        public async Task SuccessTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, CancellationToken cancellationToken = default)
        {
            var requestContent = contractSerializer.CreateJsonContent(new Models.SuccessTaskRequest { ExecutorId = executorId, TaskId = taskId, ExecutingTime = executingTime });

            var response = await httpClient.PostAsync("executor/success", requestContent, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return;
                default:
                    throw new Exception();
            }
        }

        public async Task ErrorTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, Exception exception, CancellationToken cancellationToken = default)
        {
            var requestContent = contractSerializer.CreateJsonContent(new Models.ErrorTaskRequest { ExecutorId = executorId, TaskId = taskId, ExecutingTime = executingTime });

            var response = await httpClient.PostAsync("executor/error", requestContent, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return;
                default:
                    throw new Exception();
            }
        }

        public Task DeferTaskAsync(Guid executorId, Guid taskId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}