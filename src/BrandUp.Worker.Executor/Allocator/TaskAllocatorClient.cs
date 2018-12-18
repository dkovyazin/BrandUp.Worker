using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocatorClient : ITaskAllocator
    {
        private readonly IHttpClientFactory clientFactory;

        public TaskAllocatorClient(IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public async Task<Guid> PushTaskAsync(object taskModel)
        {
            using (var client = clientFactory.CreateClient("BrandUp.Worker.Allocator"))
            {
                var response = await client.PostAsync("task", null, CancellationToken.None);
                if (response.IsSuccessStatusCode)
                    return Guid.Parse(await response.Content.ReadAsStringAsync());
                else
                    return Guid.Empty;
            }
        }

        public Task DeferTaskAsync(Guid executorId, Guid taskId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task ErrorTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, Exception exception, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> SubscribeAsync(string[] taskTypeNames, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SuccessTaskAsync(Guid executorId, Guid taskId, TimeSpan executingTime, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TaskToExecute>> WaitTasksAsync(Guid executorId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}