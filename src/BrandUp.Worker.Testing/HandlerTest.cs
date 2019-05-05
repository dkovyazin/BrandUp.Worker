using BrandUp.Worker.Allocator;
using BrandUp.Worker.Builder;
using BrandUp.Worker.Executor;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Testing
{
    public class HandlerTest<TCommand, THandler> : IDisposable
        where TCommand : class, new()
        where THandler : TaskHandler<TCommand>
    {
        protected ServiceProvider ServiceProvider { get; private set; }
        protected IServiceScope ServiceScope { get; private set; }
        protected ITaskAllocator Allocator { get; }
        protected TaskExecutor Executor { get; }
        protected ITaskService TaskService { get; }
        protected ITaskMetadataManager TaskMetadataManager { get; }

        public HandlerTest()
        {
            var services = new ServiceCollection();

            services.AddLogging();

            var workerBuilder = services.AddWorkerAllocator(OnConfigureAllocator);
            workerBuilder.AddTaskType<TCommand>();

            var executorBuilder = workerBuilder.AddExecutor();
            executorBuilder.MapTaskHandler<TCommand, THandler>();

            OnConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();
            ServiceScope = ServiceProvider.CreateScope();

            Allocator = ServiceScope.ServiceProvider.GetRequiredService<ITaskAllocator>();
            Executor = ServiceScope.ServiceProvider.GetRequiredService<TaskExecutor>();
            TaskService = ServiceScope.ServiceProvider.GetRequiredService<ITaskService>();
            TaskMetadataManager = ServiceScope.ServiceProvider.GetRequiredService<ITaskMetadataManager>();
        }

        protected virtual void OnConfigureAllocator(TaskAllocatorOptions options) { }
        protected virtual void OnConfigureServices(IServiceCollection services) { }
        public async Task<TaskExecutionStatus> ExecuteTaskAsync(TCommand task, CancellationToken cancellationToken = default)
        {
            var taskMetadata = TaskMetadataManager.FindTaskMetadata<TCommand>();

            await Executor.ConnectAsync(cancellationToken);

            var taskId = await Allocator.PushTaskAsync(task, cancellationToken);

            var tasks = await Allocator.WaitTasksAsync(Executor.ExecutorId, cancellationToken);
            var taskExecutionModel = tasks.Single();

            var executorContext = new JobExecutorContextWrapper(Executor.ExecutorId, Executor);

            using (var job = new TaskJob(taskExecutionModel, new TaskHandlerMetadata(taskMetadata.TaskName, typeof(TaskHandler<TCommand>)), ServiceScope, executorContext))
            {
                await job.Run(cancellationToken);
            }

            return executorContext.Status;
        }

        #region IDisposable members

        void IDisposable.Dispose()
        {
            ServiceScope.Dispose();
            ServiceProvider.Dispose();
        }

        #endregion
    }

    internal class JobExecutorContextWrapper : IJobExecutorContext
    {
        private readonly IJobExecutorContext executorContext;

        public TaskExecutionStatus Status { get; private set; }

        public JobExecutorContextWrapper(Guid executorId, IJobExecutorContext executorContext)
        {
            ExecutorId = executorId;
            this.executorContext = executorContext;
        }

        #region IJobExecutorContext members

        public Guid ExecutorId { get; }

        public Task OnDefferJob(TaskJob job)
        {
            Status = TaskExecutionStatus.Deffer;

            return executorContext.OnDefferJob(job);
        }

        public Task OnErrorJob(TaskJob job, Exception exception)
        {
            Status = TaskExecutionStatus.Error;

            return executorContext.OnErrorJob(job, exception);
        }

        public Task OnStartedJob(TaskJob job)
        {
            Status = TaskExecutionStatus.Started;

            return executorContext.OnStartedJob(job);
        }

        public Task OnSuccessJob(TaskJob job)
        {
            Status = TaskExecutionStatus.Success;

            return executorContext.OnSuccessJob(job);
        }

        public Task OnTimeoutJob(TaskJob job)
        {
            Status = TaskExecutionStatus.Timeout;

            return executorContext.OnTimeoutJob(job);
        }

        public Task OnUnhandledError(TaskJob job, Exception exception)
        {
            Status = TaskExecutionStatus.UnhandledError;

            return executorContext.OnUnhandledError(job, exception);
        }

        #endregion
    }

    public enum TaskExecutionStatus
    {
        Started,
        Deffer,
        Error,
        Success,
        Timeout,
        UnhandledError
    }
}