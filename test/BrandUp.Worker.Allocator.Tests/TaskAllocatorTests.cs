using BrandUp.Worker.Builder;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Allocator.Infrastructure
{
    public class TaskAllocatorTests : IDisposable
    {
        private readonly ServiceProvider serviceProvider;
        private readonly IServiceScope serviceScope;
        private readonly ITaskMetadataManager manager;
        private readonly TaskAllocator allocator;

        public TaskAllocatorTests()
        {
            var services = new ServiceCollection();
            services.AddWorker()
                .AddTaskType(typeof(TestTask))
                .AddAllocator(options =>
                {
                    options.DefaultTaskWaitingTimeout = TimeSpan.FromSeconds(2);
                });

            serviceProvider = services.BuildServiceProvider();
            serviceScope = serviceProvider.CreateScope();

            manager = serviceScope.ServiceProvider.GetService<ITaskMetadataManager>();
            allocator = new TaskAllocator(manager, new DefaultTaskRepository(), serviceScope.ServiceProvider.GetService<IOptions<TaskAllocatorOptions>>());
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
            var result = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));

            Assert.True(result.Success);
            Assert.NotEqual(result.ExecutorId, Guid.Empty);
            Assert.Equal(1, allocator.CountExecutors);
        }

        [Fact]
        public void PushCommand()
        {
            var taskId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            Assert.False(isStarted);
            Assert.Equal(executorId, Guid.Empty);
            Assert.Equal(1, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task PushCommand_AfterWaiting()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));

            var task = Task.Run(() =>
            {
                var result = allocator.WaitTasks(executorConnection.ExecutorId).Result;
                Assert.Single(result.Commands);
                Assert.Equal(0, allocator.CountCommandInQueue);
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            var taskId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.True(isStarted);
            Assert.Equal(executorId, executorConnection.ExecutorId);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task CancelHandlerWaitsAfterDisposing()
        {
            var commandNames = manager.Tasks.Select(it => it.TaskName).ToArray();
            var workerConnectionResult = allocator.ConnectExecutor(new ExecutorOptions(commandNames));

            var task = Task.Run(() =>
            {
                var waitCommandsResult = allocator.WaitTasksAsync(workerConnectionResult.ExecutorId, CancellationToken.None, TimeSpan.FromSeconds(5));
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(1, allocator.CountExecutorWaitings);

            allocator.Dispose();

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.True(task.IsCompleted);
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitCommands_DefaultTimeout()
        {
            var result = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));

            var w = new System.Diagnostics.Stopwatch();
            w.Start();

            await allocator.WaitTasks(result.ExecutorId);

            w.Stop();

            Thread.Sleep(1000);

            Assert.True(w.Elapsed >= TimeSpan.FromSeconds(1));
            Assert.True(w.Elapsed < TimeSpan.FromSeconds(3));
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitCommands_CustomTimeout()
        {
            var result = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));

            var w = new System.Diagnostics.Stopwatch();
            w.Start();

            await allocator.WaitTasks(result.ExecutorId, TimeSpan.FromSeconds(1));

            w.Stop();

            Assert.True(w.Elapsed >= TimeSpan.FromSeconds(1));
            Assert.True(w.Elapsed < TimeSpan.FromSeconds(2));
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitCommands_Cancel()
        {
            var result = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));

            var w = new System.Diagnostics.Stopwatch();
            w.Start();

            var cancellation = new CancellationTokenSource();
            var task = Task.Run(() =>
            {
                allocator.WaitTasks(result.ExecutorId, cancellation.Token).Wait();
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            cancellation.Cancel();

            w.Stop();

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.True(task.IsCompleted);
            Assert.True(w.Elapsed >= TimeSpan.FromSeconds(1));
            Assert.True(w.Elapsed < TimeSpan.FromSeconds(2));
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitCommands_ReturnOneCommand()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));
            var commandId = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            var result = await allocator.WaitTasks(executorConnection.ExecutorId);

            Assert.Single(result.Commands);
            Assert.Equal(result.Commands[0].CommandId, commandId);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(1, allocator.CountCommandExecuting);
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitCommands_ReturnSeveralCommand()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));
            var commandId1 = allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);
            var commandId2 = allocator.PushTask(new TestTask(), out isStarted, out executorId);

            var result = await allocator.WaitTasks(executorConnection.ExecutorId);

            Assert.Equal(2, result.Commands.Count);
            Assert.Equal(result.Commands[0].CommandId, commandId1);
            Assert.Equal(result.Commands[1].CommandId, commandId2);
            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(2, allocator.CountCommandExecuting);
            Assert.Equal(0, allocator.CountExecutorWaitings);
        }

        [Fact]
        public async Task WaitCommands_Cycle()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));

            int i;
            for (i = 1; i <= 2; i++)
            {
                await Task.Run(() => allocator.WaitTasks(executorConnection.ExecutorId));
            }

            Assert.Equal(3, i);
        }

        [Fact]
        public async Task EndCommandExecuting()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));
            allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            var waitResult = await allocator.WaitTasks(executorConnection.ExecutorId);

            await allocator.SuccessTaskAsync(executorConnection.ExecutorId, waitResult.Commands[0].CommandId, TimeSpan.FromSeconds(1), CancellationToken.None);

            Assert.Equal(0, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }

        [Fact]
        public async Task ReturnCommandToQueue()
        {
            var executorConnection = allocator.ConnectExecutor(new ExecutorOptions(manager.Tasks.Select(it => it.TaskName).ToArray()));
            allocator.PushTask(new TestTask(), out bool isStarted, out Guid executorId);

            var waitResult = await allocator.WaitTasks(executorConnection.ExecutorId);

            var result = allocator.ReturnCommandToQueue(executorConnection.ExecutorId, waitResult.Commands[0].CommandId);

            Assert.True(result);
            Assert.Equal(1, allocator.CountCommandInQueue);
            Assert.Equal(0, allocator.CountCommandExecuting);
        }
    }
}