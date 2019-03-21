using ContosoWorker.Service;
using ContosoWorker.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Allocator
{
    public class RemoteTaskAllocatorTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> factory;

        public RemoteTaskAllocatorTests(WebApplicationFactory<Startup> factory)
        {
            this.factory = factory;
            this.factory.ClientOptions.BaseAddress = new Uri("http://localhost/");
        }

        [Fact]
        public async Task PushTask()
        {
            var client = factory.CreateClient();
            var contractSerializer = factory.Server.Host.Services.GetRequiredService<Remoting.IContractSerializer>();

            var tasksClient = new RemoteTaskAllocator(client, contractSerializer);

            // Act
            var task = new TestTask();
            var taskId = await tasksClient.PushTaskAsync(task);

            Assert.NotEqual(Guid.Empty, taskId);
        }

        [Fact]
        public async Task Subscribe()
        {
            var client = factory.CreateClient();
            var contractSerializer = factory.Server.Host.Services.GetRequiredService<Remoting.IContractSerializer>();
            var taskMetadataManager = factory.Server.Host.Services.GetRequiredService<Tasks.ITaskMetadataManager>();

            var tasksClient = new RemoteTaskAllocator(client, contractSerializer);

            // Act
            var executorId = await tasksClient.SubscribeAsync(taskMetadataManager.Tasks.Select(it => it.TaskName).ToArray());

            Assert.NotEqual(Guid.Empty, executorId);
        }

        [Fact]
        public async Task Wait_Empty()
        {
            var client = factory.CreateClient();
            var contractSerializer = factory.Server.Host.Services.GetRequiredService<Remoting.IContractSerializer>();
            var taskMetadataManager = factory.Server.Host.Services.GetRequiredService<Tasks.ITaskMetadataManager>();

            var tasksClient = new RemoteTaskAllocator(client, contractSerializer);

            // Act
            var executorId = await tasksClient.SubscribeAsync(taskMetadataManager.Tasks.Select(it => it.TaskName).ToArray());
            var tasks = await tasksClient.WaitTasksAsync(executorId);

            Assert.Empty(tasks);
        }

        [Fact]
        public async Task Wait_NoEmpty()
        {
            var client = factory.CreateClient();
            var contractSerializer = factory.Server.Host.Services.GetRequiredService<Remoting.IContractSerializer>();
            var taskMetadataManager = factory.Server.Host.Services.GetRequiredService<Tasks.ITaskMetadataManager>();

            var tasksClient = new RemoteTaskAllocator(client, contractSerializer);

            // Act
            var taskId = await tasksClient.PushTaskAsync(new TestTask());
            var executorId = await tasksClient.SubscribeAsync(taskMetadataManager.Tasks.Select(it => it.TaskName).ToArray());
            var tasks = await tasksClient.WaitTasksAsync(executorId);

            Assert.NotEmpty(tasks);
            Assert.Contains(tasks, it => it.TaskId == taskId);
        }
    }
}