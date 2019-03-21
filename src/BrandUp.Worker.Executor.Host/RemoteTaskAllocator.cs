using BrandUp.Worker.Remoting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public class RemoteTaskAllocator : ITaskAllocator
    {
        private readonly HttpClient httpClient;
        private readonly IContractSerializer contractSerializer;

        public RemoteTaskAllocator(HttpClient httpClient, IContractSerializer contractSerializer)
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

        public Task DeferTaskAsync(Guid executorId, Guid taskId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
    }
}