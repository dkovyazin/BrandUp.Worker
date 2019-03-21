using ContosoWorker.Service;
using ContosoWorker.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Allocator
{
    public class RemoteTaskServiceTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> factory;

        public RemoteTaskServiceTests(WebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
            this.factory.ClientOptions.BaseAddress = new Uri("http://localhost/");
        }

        [Fact]
        public async Task PushTask()
        {
            var client = factory.CreateClient();
            var contractSerializer = factory.Server.Host.Services.GetRequiredService<Remoting.IContractSerializer>();

            var tasksClient = new RemoteTaskService(client, contractSerializer);

            // Act
            var task = new TestTask();
            var taskId = await tasksClient.PushTaskAsync(task);

            Assert.NotEqual(Guid.Empty, taskId);
        }
    }
}