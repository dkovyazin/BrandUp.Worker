using BrandUp.Worker.Builder;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocatorAfterRestoreTests : IAsyncLifetime
    {
        private readonly IHost host;
        private readonly ITaskMetadataManager manager;
        private readonly TaskAllocator allocator;
        private readonly MemoryTaskRepository taskRepository;

        public TaskAllocatorAfterRestoreTests()
        {
            host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddWorkerAllocator(options =>
                        {
                            options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
                        })
                        .AddTaskRepository<MemoryTaskRepository>()
                        .AddTaskType(typeof(TestTask));

                    services.AddSingleton<TaskAllocator>();
                })
                .Build();

            manager = host.Services.GetService<ITaskMetadataManager>();
            allocator = (TaskAllocator)host.Services.GetRequiredService<ITaskAllocator>();
            taskRepository = (MemoryTaskRepository)host.Services.GetRequiredService<ITaskRepository>();
        }

        #region IAsyncLifetime members

        async Task IAsyncLifetime.InitializeAsync()
        {
            var taskId = Guid.NewGuid();
            await taskRepository.PushTaskAsync(taskId, "TestTask", new TestTask(), DateTime.UtcNow);

            taskId = Guid.NewGuid();
            await taskRepository.PushTaskAsync(taskId, "TestTask", new TestTask(), DateTime.UtcNow);
            await taskRepository.TaskStartedAsync(taskId, Guid.NewGuid(), DateTime.UtcNow);

            await host.StartAsync();
        }
        Task IAsyncLifetime.DisposeAsync()
        {
            return host.StopAsync();
        }

        #endregion

        [Fact]
        public void ResporeTasks()
        {
            Assert.Equal(1, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
        }
    }
}