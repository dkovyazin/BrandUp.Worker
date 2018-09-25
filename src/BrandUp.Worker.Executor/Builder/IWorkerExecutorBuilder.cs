using BrandUp.Worker.Executor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace BrandUp.Worker.Builder
{
    public class WorkerExecutorBuilder : IWorkerExecutorBuilder, ITaskHandlerManager
    {
        private readonly IWorkerBuilder workerBuilder;
        private readonly List<Type> taskTypes = new List<Type>();

        public WorkerExecutorBuilder(IWorkerBuilder workerBuilder)
        {
            this.workerBuilder = workerBuilder ?? throw new ArgumentNullException(nameof(workerBuilder));

            workerBuilder.Services.AddSingleton<ITaskHandlerManager>(this);
            workerBuilder.Services.AddSingleton<TaskExecutor>();
            workerBuilder.Services.AddHostedService<TaskExecutorHostService>();
        }

        public IServiceCollection Services => workerBuilder.Services;
        public IWorkerExecutorBuilder MapTaskHandler<TTask, THandler>()
            where TTask : class, new()
            where THandler : TaskHandler<TTask>
        {
            var taskMetadata = workerBuilder.TaskMetadataManager.FindTaskMetadata<TTask>();
            if (taskMetadata == null)
                throw new ArgumentException();

            if (taskTypes.Contains(typeof(TTask)))
                throw new ArgumentException();

            taskTypes.Add(typeof(TTask));
            Services.AddTaskHandler<TTask, THandler>();

            return this;
        }

        IEnumerable<Type> ITaskHandlerManager.TaskTypes => taskTypes;
    }

    public interface IWorkerExecutorBuilder
    {
        IServiceCollection Services { get; }
        IWorkerExecutorBuilder MapTaskHandler<TTask, THandler>()
            where TTask : class, new()
            where THandler : TaskHandler<TTask>;
    }
}