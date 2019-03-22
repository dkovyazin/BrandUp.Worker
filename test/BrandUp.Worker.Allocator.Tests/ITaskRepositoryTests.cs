using BrandUp.Worker.Builder;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Allocator
{
    public class ITaskRepositoryTests : IAsyncLifetime
    {
        private readonly IHost host;
        private readonly ITaskMetadataManager metadataManager;
        private readonly ITaskAllocator allocator;
        private readonly MemoryTaskRepository taskRepository;

        public ITaskRepositoryTests()
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

            metadataManager = host.Services.GetRequiredService<ITaskMetadataManager>();
            allocator = host.Services.GetRequiredService<ITaskAllocator>();
            taskRepository = (MemoryTaskRepository)host.Services.GetRequiredService<ITaskRepository>();
        }

        #region IAsyncLifetime members

        Task IAsyncLifetime.InitializeAsync()
        {
            return host.StartAsync();
        }
        Task IAsyncLifetime.DisposeAsync()
        {
            return host.StopAsync();
        }

        #endregion

        #region Test methods

        [Fact]
        public async Task PushTask()
        {
            var taskId = await allocator.PushTaskAsync(new TestTask());

            Assert.NotEmpty(taskRepository.Tasks);
            Assert.Contains(taskRepository.TaskIds, it => it == taskId);
        }

        [Fact]
        public async Task TaskStarted()
        {
            var taskId = await allocator.PushTaskAsync(new TestTask());
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            await allocator.WaitTasksAsync(executorId);

            Assert.NotEmpty(taskRepository.Tasks);
            Assert.Contains(taskRepository.TaskIds, it => it == taskId);

            var task = taskRepository.Tasks.Single();
            Assert.NotNull(task.Execution);
            Assert.Equal(TaskExecutionStatus.Started, task.Execution.Status);
            Assert.Equal(executorId, task.Execution.ExecutorId);
        }

        [Fact]
        public async Task TaskDefered()
        {
            var taskId = await allocator.PushTaskAsync(new TestTask());
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            await allocator.WaitTasksAsync(executorId);

            await allocator.DeferTaskAsync(executorId, taskId);

            Assert.NotEmpty(taskRepository.Tasks);
            Assert.Contains(taskRepository.TaskIds, it => it == taskId);

            var task = taskRepository.Tasks.Single();
            Assert.Null(task.Execution);
        }

        [Fact]
        public async Task TaskError()
        {
            var taskId = await allocator.PushTaskAsync(new TestTask());
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            await allocator.WaitTasksAsync(executorId);

            await allocator.ErrorTaskAsync(executorId, taskId, TimeSpan.FromSeconds(1), null);

            Assert.NotEmpty(taskRepository.Tasks);
            Assert.Contains(taskRepository.TaskIds, it => it == taskId);

            var task = taskRepository.Tasks.Single();
            Assert.NotNull(task.EndDate);
            Assert.NotNull(task.Execution);
            Assert.Equal(TaskExecutionStatus.Error, task.Execution.Status);
            Assert.NotNull(task.Execution.ExecutionTime);
            Assert.Equal(executorId, task.Execution.ExecutorId);
        }

        [Fact]
        public async Task TaskDone()
        {
            var taskId = await allocator.PushTaskAsync(new TestTask());
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            await allocator.WaitTasksAsync(executorId);

            await allocator.SuccessTaskAsync(executorId, taskId, TimeSpan.FromSeconds(1));

            Assert.NotEmpty(taskRepository.Tasks);
            Assert.Contains(taskRepository.TaskIds, it => it == taskId);

            var task = taskRepository.Tasks.Single();
            Assert.NotNull(task.EndDate);
            Assert.NotNull(task.Execution);
            Assert.Equal(TaskExecutionStatus.Success, task.Execution.Status);
            Assert.NotNull(task.Execution.ExecutionTime);
            Assert.Equal(executorId, task.Execution.ExecutorId);
        }

        [Fact]
        public async Task TaskCancelled()
        {
            var taskId = await allocator.PushTaskAsync(new TestTask());

            await Task.Delay(1000);

            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            await allocator.WaitTasksAsync(executorId);

            Assert.NotEmpty(taskRepository.Tasks);
            Assert.Contains(taskRepository.TaskIds, it => it == taskId);

            var task = taskRepository.Tasks.Single();
            Assert.NotNull(task.EndDate);
            Assert.Null(task.Execution);
        }

        #endregion
    }
}