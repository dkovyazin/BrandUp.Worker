using BrandUp.Worker.Remoting;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker
{
    public class RemoteTaskService : ITaskService
    {
        private readonly HttpClient httpClient;
        private readonly IContractSerializer contractSerializer;

        public RemoteTaskService(HttpClient httpClient, IContractSerializer contractSerializer)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.contractSerializer = contractSerializer ?? throw new ArgumentNullException(nameof(contractSerializer));
        }

        #region ITaskService members

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

        #endregion
    }

    public class JsonContent<T> : HttpContent
        where T : class
    {
        private readonly T data;
        private readonly IContractSerializer contractSerializer;

        public JsonContent(T data, IContractSerializer contractSerializer)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            this.contractSerializer = contractSerializer ?? throw new ArgumentNullException(nameof(contractSerializer));

            Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contractSerializer.ContentType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            contractSerializer.Serialize(stream, data, false);

            return Task.CompletedTask;
        }
        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}