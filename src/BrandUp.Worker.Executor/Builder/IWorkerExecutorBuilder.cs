using BrandUp.Worker.Executor;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace BrandUp.Worker.Builder
{
    public class WorkerExecutorBuilder : IWorkerExecutorBuilder, ITaskHandlerManager
    {
        private readonly IWorkerBuilderCore workerBuilder;
        private readonly List<Type> taskTypes = new List<Type>();

        public WorkerExecutorBuilder(IWorkerBuilderCore workerBuilder)
        {
            this.workerBuilder = workerBuilder ?? throw new ArgumentNullException(nameof(workerBuilder));

            workerBuilder.Services.AddSingleton<ITaskHandlerManager>(this);
            workerBuilder.Services.AddSingleton<TaskExecutor>();
        }

        public IServiceCollection Services => workerBuilder.Services;
        public IWorkerExecutorBuilder MapTaskHandler<TTask, THandler>()
            where TTask : class, new()
            where THandler : TaskHandler<TTask>
        {
            var taskMetadata = workerBuilder.TasksMetadata.FindTaskMetadata<TTask>();
            if (taskMetadata == null)
                throw new ArgumentException();

            if (taskTypes.Contains(typeof(TTask)))
                throw new ArgumentException();

            taskTypes.Add(typeof(TTask));
            Services.AddTransient<TaskHandler<TTask>, THandler>();

            return this;
        }
        public IWorkerBuilderCore AddTaskType(Type taskType)
        {
            return workerBuilder.AddTaskType(taskType);
        }
        public ITaskMetadataManager TasksMetadata => workerBuilder.TasksMetadata;

        IEnumerable<Type> ITaskHandlerManager.TaskTypes => taskTypes;
    }

    public interface IWorkerExecutorBuilder : IWorkerBuilderCore
    {
        IWorkerExecutorBuilder MapTaskHandler<TTask, THandler>()
            where TTask : class, new()
            where THandler : TaskHandler<TTask>;
    }
}