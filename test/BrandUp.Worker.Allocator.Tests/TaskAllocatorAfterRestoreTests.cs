using BrandUp.Worker.Builder;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocatorAfterRestoreTests : IAsyncLifetime
    {
        private readonly ServiceProvider serviceProvider;
        private readonly IServiceScope serviceScope;
        private readonly ITaskMetadataManager manager;
        private readonly TaskAllocator allocator;
        private readonly MemoryTaskRepository taskRepository;

        public TaskAllocatorAfterRestoreTests()
        {
            var services = new ServiceCollection();
            services.AddWorker()
                .AddTaskType(typeof(TestTask))
                .AddAllocator(options =>
                {
                    options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
                });

            serviceProvider = services.BuildServiceProvider();
            serviceScope = serviceProvider.CreateScope();

            manager = serviceScope.ServiceProvider.GetService<ITaskMetadataManager>();
            taskRepository = new MemoryTaskRepository();
            allocator = new TaskAllocator(manager, taskRepository, serviceScope.ServiceProvider.GetService<IOptions<TaskAllocatorOptions>>());
        }

        async Task IAsyncLifetime.InitializeAsync()
        {
            var taskId = Guid.NewGuid();
            await taskRepository.PushTaskAsync(taskId, new TestTask(), DateTime.UtcNow);

            taskId = Guid.NewGuid();
            await taskRepository.PushTaskAsync(taskId, new TestTask(), DateTime.UtcNow);
            await taskRepository.TaskStartedAsync(taskId, Guid.NewGuid(), DateTime.UtcNow);
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            allocator.Dispose();
            serviceScope.Dispose();
            serviceProvider.Dispose();

            return Task.CompletedTask;
        }

        [Fact]
        public async Task ResporeTasks()
        {
            await allocator.ResporeTasksAsync();

            Assert.Equal(1, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
        }
    }
}