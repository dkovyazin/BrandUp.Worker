using BrandUp.Worker.Builder;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocatorTests : IAsyncLifetime
    {
        private readonly IHost host;
        private readonly ITaskMetadataManager metadataManager;
        private readonly TaskAllocator allocator;

        public TaskAllocatorTests()
        {
            host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddWorkerAllocator(options =>
                        {
                            options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
                        })
                        .AddTaskType<TestTask>();

                    services.AddSingleton<TaskAllocator>();
                })
                .Build();

            metadataManager = host.Services.GetRequiredService<ITaskMetadataManager>();
            allocator = (TaskAllocator)host.Services.GetRequiredService<ITaskAllocator>();
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
        public async Task Subscribe()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());

            Assert.NotEqual(executorId, Guid.Empty);
            Assert.Equal(1, allocator.CountExecutors);
        }

        [Fact]
        public async Task PushTask_And_Wait()
        {
            var taskId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            Assert.False(isStarted);
            Assert.Equal(executorId, Guid.Empty);
            Assert.Equal(1, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);

            executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            var tasksToExecute = await allocator.WaitTasksAsync(executorId, CancellationToken.None);

            Assert.NotEmpty(tasksToExecute);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task PushTask_AfterWaiting()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());

            var task = Task.Run(() =>
            {
                var tasksToExecute = allocator.WaitTasksAsync(executorId, CancellationToken.None).Result;
                Assert.Single(tasksToExecute);
                Assert.Equal(0, allocator.CountCommandInQueue);
            });

            await Task.Delay(500);

            var taskId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid taskExecutorId);

            Assert.True(isStarted);
            Assert.Equal(taskExecutorId, executorId);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task CancelHandlerWaitsAfterDisposing()
        {
            var commandNames = metadataManager.Tasks.Select(it => it.TaskName).ToArray();
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());

            var task = Task.Run(() =>
            {
                allocator.WaitTasksAsync(executorId, CancellationToken.None).Wait();
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(1, allocator.CountExecutorWaitings);

            allocator.Dispose();

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.True(task.IsCompleted);
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitTasks_DefaultTimeout()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());

            var w = new System.Diagnostics.Stopwatch();
            w.Start();

            await allocator.WaitTasksAsync(executorId, CancellationToken.None);

            w.Stop();

            Thread.Sleep(1000);

            Assert.True(w.Elapsed >= TimeSpan.FromSeconds(1));
            Assert.True(w.Elapsed < TimeSpan.FromSeconds(3));
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitTasks_Cancel()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());

            using (var cancellation = new CancellationTokenSource())
            {
                var task = Task.Run(() => allocator.WaitTasksAsync(executorId, cancellation.Token));

                await Task.Delay(200);

                cancellation.Cancel();

                Assert.Equal(0, allocator.CountExecutorWaitings);
            }
        }

        [Fact]
        public async Task WaitTasks_ReturnOneCommand()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            var commandId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid taskExecutorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorId, CancellationToken.None)).ToList();

            Assert.Single(tasksToExecute);
            Assert.Equal(tasksToExecute[0].TaskId, commandId);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitTasks_ReturnSeveralCommand()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            var commandId1 = allocator.PushTask(new TestTask(), out bool isStarted, out Guid taskExecutorId);
            var commandId2 = allocator.PushTask(new TestTask(), out isStarted, out taskExecutorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorId, CancellationToken.None)).ToList();

            Assert.Equal(2, tasksToExecute.Count);
            Assert.Equal(tasksToExecute[0].TaskId, commandId1);
            Assert.Equal(tasksToExecute[1].TaskId, commandId2);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(2, allocator.CountCommandExecuting);
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitTasks_TimeoutWaitingToStart()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            var commandId1 = allocator.PushTask(new TestTask(), out bool isStarted, out Guid taskExecutorId);

            await Task.Delay(500);

            var tasksToExecute = await allocator.WaitTasksAsync(executorId, CancellationToken.None);

            Assert.Empty(tasksToExecute);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task WaitTasks_Cycle()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());

            int i;
            for (i = 1; i <= 2; i++)
            {
                await Task.Run(() => allocator.WaitTasksAsync(executorId, CancellationToken.None));
            }

            Assert.Equal(3, i);
        }

        [Fact]
        public async Task SuccessTask()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            allocator.PushTask(new TestTask(), out bool isStarted, out Guid taskExecutorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorId, CancellationToken.None)).ToList();

            await allocator.SuccessTaskAsync(executorId, tasksToExecute[0].TaskId, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task ErrorTask()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            allocator.PushTask(new TestTask(), out bool isStarted, out Guid taskExecutorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorId, CancellationToken.None)).ToList();

            await allocator.ErrorTaskAsync(executorId, tasksToExecute[0].TaskId, TimeSpan.FromSeconds(1), new Exception("Error"), CancellationToken.None);

            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task DeferTask()
        {
            var executorId = await allocator.SubscribeAsync(metadataManager.Tasks.Select(it => it.TaskName).ToArray());
            allocator.PushTask(new TestTask(), out bool isStarted, out Guid taskExecutorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorId, CancellationToken.None)).ToList();

            await allocator.DeferTaskAsync(executorId, tasksToExecute[0].TaskId, CancellationToken.None);

            Assert.Equal(1, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        #endregion
    }
}