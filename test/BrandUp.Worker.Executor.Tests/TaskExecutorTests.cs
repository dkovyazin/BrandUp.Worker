using BrandUp.Worker.Builder;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Executor.Tests
{
    public class TaskExecutorTests : IAsyncLifetime
    {
        private readonly IHost host;
        private readonly Allocator.ITaskAllocator allocator;
        private readonly TaskExecutor taskExecutor;

        public TaskExecutorTests()
        {
            host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddWorkerAllocator(options =>
                        {
                            options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(3);
                        })
                        .AddTaskType<SuccessTask>()
                        .AddTaskType<ErrorTask>()
                        .AddTaskType<TimeoutTask>()
                        .AddLocalExecutor()
                            .MapTaskHandler<SuccessTask, SuccessTaskHandler>()
                            .MapTaskHandler<ErrorTask, ErrorTaskHandler>()
                            .MapTaskHandler<TimeoutTask, TimeoutTaskHandler>();

                })
                .Build();

            allocator = host.Services.GetService<Allocator.ITaskAllocator>();
            taskExecutor = host.Services.GetService<TaskExecutor>();
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
        public async Task ExecuteTask_Success()
        {
            Assert.True(taskExecutor.IsStarted);

            var taskId = await allocator.PushTaskAsync(new SuccessTask());

            Thread.Sleep(1000);

            Assert.Equal(1, taskExecutor.ExecutedCommands);
            Assert.Equal(0, taskExecutor.ExecutingCommands);
            Assert.Equal(0, taskExecutor.FaultedCommands);
            Assert.Equal(0, taskExecutor.CancelledCommands);
        }

        [Fact]
        public async Task ExecuteTask_Error()
        {
            Assert.True(taskExecutor.IsStarted);

            var taskId = await allocator.PushTaskAsync(new ErrorTask());

            Thread.Sleep(1000);

            Assert.Equal(1, taskExecutor.ExecutedCommands);
            Assert.Equal(0, taskExecutor.ExecutingCommands);
            Assert.Equal(1, taskExecutor.FaultedCommands);
            Assert.Equal(0, taskExecutor.CancelledCommands);
        }

        [Fact]
        public async Task ExecuteTask_Timeout()
        {
            Assert.True(taskExecutor.IsStarted);

            var taskId = await allocator.PushTaskAsync(new TimeoutTask());

            await Task.Delay(300);

            Assert.Equal(1, taskExecutor.ExecutedCommands);
            Assert.Equal(0, taskExecutor.ExecutingCommands);
            Assert.Equal(1, taskExecutor.FaultedCommands);
            Assert.Equal(0, taskExecutor.CancelledCommands);
        }

        [Fact]
        public async Task ExecuteTask_HostStopped()
        {
            Assert.True(taskExecutor.IsStarted);

            var taskId = await allocator.PushTaskAsync(new SuccessTask());

            await Task.Delay(300);

            await host.StopAsync();

            await Task.Delay(300);

            Assert.Equal(0, taskExecutor.ExecutedCommands);
            Assert.Equal(0, taskExecutor.ExecutingCommands);
            Assert.Equal(0, taskExecutor.FaultedCommands);
            Assert.Equal(1, taskExecutor.CancelledCommands);
        }

        #endregion
    }
}