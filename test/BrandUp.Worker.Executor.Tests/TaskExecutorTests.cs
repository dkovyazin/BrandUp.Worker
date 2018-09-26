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
                    var workerBuilder = services.AddWorker()
                        .AddTaskType<SuccessTask>()
                        .AddTaskType<ErrorTask>()
                        .AddAllocator(options =>
                        {
                            options.DefaultTaskWaitingTimeout = TimeSpan.FromSeconds(3);
                        });

                    workerBuilder.AddExecutor()
                        .MapTaskHandler<SuccessTask, SuccessTaskHandler>()
                        .MapTaskHandler<ErrorTask, ErrorTaskHandler>();

                })
                .Build();

            taskService = host.Services.GetService<ITaskService>();
            taskExecutor = host.Services.GetService<TaskExecutor>();
        }

        Task IAsyncLifetime.InitializeAsync()
        {
            return host.StartAsync();
        }
        Task IAsyncLifetime.DisposeAsync()
        {
            return host.StopAsync();
        }

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
    }

    public class SuccessTaskHandler : TaskHandler<SuccessTask>
    {
        protected override Task OnWorkAsync(SuccessTask command, CancellationToken cancellationToken)
        {
            return Task.Delay(500);
        }
    }

    public class ErrorTaskHandler : TaskHandler<ErrorTask>
    {
        protected override Task OnWorkAsync(ErrorTask command, CancellationToken cancellationToken)
        {
            throw new Exception("Error");
        }
    }
}