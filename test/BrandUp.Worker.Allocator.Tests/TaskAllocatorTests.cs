using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocatorTests : IDisposable
    {
        private readonly ServiceProvider serviceProvider;
        private readonly IServiceScope serviceScope;
        private readonly ITaskMetadataManager metadataManager;
        private readonly TaskAllocator allocator;

        public TaskAllocatorTests()
        {
            var services = new ServiceCollection();
            services
                .AddWorkerAllocator(options =>
                {
                    options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
                })
                .AddTaskType(typeof(TestTask));

            serviceProvider = services.BuildServiceProvider();
            serviceScope = serviceProvider.CreateScope();

            metadataManager = serviceScope.ServiceProvider.GetService<ITaskMetadataManager>();
            allocator = new TaskAllocator(metadataManager, new MemoryTaskRepository(), serviceScope.ServiceProvider.GetService<IOptions<TaskAllocatorOptions>>());
        }

        void IDisposable.Dispose()
        {
            allocator.Dispose();
            serviceScope.Dispose();
            serviceProvider.Dispose();
        }

        [Fact]
        public void ConnectExecutor()
        {
            var result = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));

            Assert.True(result.Success);
            Assert.NotEqual(result.ExecutorId, Guid.Empty);
            Assert.Equal(1, allocator.CountExecutors);
        }

        [Fact]
        public async Task PushTask()
        {
            var taskId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            Assert.False(isStarted);
            Assert.Equal(executorId, Guid.Empty);
            Assert.Equal(1, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);

            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));
            var tasksToExecute = await allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None);

            Assert.NotEmpty(tasksToExecute);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task PushTask_AfterWaiting()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));

            var task = Task.Run(() =>
            {
                var tasksToExecute = allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None).Result;
                Assert.Single(tasksToExecute);
                Assert.Equal(0, allocator.CountCommandInQueue);
            });

            await Task.Delay(500);

            var taskId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            Assert.True(isStarted);
            Assert.Equal(executorId, executorConnection.ExecutorId);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task CancelHandlerWaitsAfterDisposing()
        {
            var commandNames = metadataManager.Tasks.Select(it => it.TaskName).ToArray();
            var workerConnectionResult = allocator.ConnectExecutor(new ExecutorOptions(commandNames));

            var task = Task.Run(() =>
            {
                allocator.WaitTasksAsync(workerConnectionResult.ExecutorId, CancellationToken.None).Wait();
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
            var result = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));

            var w = new System.Diagnostics.Stopwatch();
            w.Start();

            await allocator.WaitTasksAsync(result.ExecutorId, CancellationToken.None);

            w.Stop();

            Thread.Sleep(1000);

            Assert.True(w.Elapsed >= TimeSpan.FromSeconds(1));
            Assert.True(w.Elapsed < TimeSpan.FromSeconds(3));
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitTasks_Cancel()
        {
            var result = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));

            using (var cancellation = new CancellationTokenSource())
            {
                var task = Task.Run(() => allocator.WaitTasksAsync(result.ExecutorId, cancellation.Token));

                await Task.Delay(200);

                cancellation.Cancel();

                Assert.Equal(0, allocator.CountExecutorWaitings);
            }
        }

        [Fact]
        public async Task WaitTasks_ReturnOneCommand()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));
            var commandId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None)).ToList();

            Assert.Single(tasksToExecute);
            Assert.Equal(tasksToExecute[0].TaskId, commandId);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitTasks_ReturnSeveralCommand()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));
            var commandId1 = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);
            var commandId2 = allocator.PushTask(new TestTask(), out isStarted, out executorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None)).ToList();

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
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));
            var commandId1 = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            await Task.Delay(500);

            var tasksToExecute = await allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None);

            Assert.Empty(tasksToExecute);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task WaitTasks_Cycle()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));

            int i;
            for (i = 1; i <= 2; i++)
            {
                await Task.Run(() => allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None));
            }

            Assert.Equal(3, i);
        }

        [Fact]
        public async Task SuccessTask()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));
            allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None)).ToList();

            await allocator.SuccessTaskAsync(executorConnection.ExecutorId, tasksToExecute[0].TaskId, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task ErrorTask()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));
            allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None)).ToList();

            await allocator.ErrorTaskAsync(executorConnection.ExecutorId, tasksToExecute[0].TaskId, TimeSpan.FromSeconds(1), new Exception("Error"), CancellationToken.None);

            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task DeferTask()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(metadataManager.Tasks.Select(it => it.TaskName).ToArray()));
            allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            var tasksToExecute = (await allocator.WaitTasksAsync(executorConnection.ExecutorId, CancellationToken.None)).ToList();

            await allocator.DeferTaskAsync(executorConnection.ExecutorId, tasksToExecute[0].TaskId, CancellationToken.None);

            Assert.Equal(1, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }
    }
}