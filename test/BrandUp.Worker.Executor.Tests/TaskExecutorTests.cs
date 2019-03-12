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
        private readonly ITaskService taskService;
        private readonly TaskExecutor taskExecutor;

        public TaskExecutorTests()
        {
            host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddWorkerCore()
                        .AddTaskType<SuccessTask>()
                        .AddTaskType<ErrorTask>()
                        .AddTaskType<TimeoutTask>()
                        .AddAllocatorHost(options =>
                        {
                            options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(3);
                        })
                        .AddExecutor()
                            .MapTaskHandler<SuccessTask, SuccessTaskHandler>()
                            .MapTaskHandler<ErrorTask, ErrorTaskHandler>()
                            .MapTaskHandler<TimeoutTask, TimeoutTaskHandler>();

                })
                .Build();

            taskService = host.Services.GetService<ITaskService>();
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

            var taskId = await taskService.PushTask(new SuccessTask());

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

            var taskId = await taskService.PushTask(new ErrorTask());

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

            var taskId = await taskService.PushTask(new TimeoutTask());

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

            var taskId = await taskService.PushTask(new SuccessTask());

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