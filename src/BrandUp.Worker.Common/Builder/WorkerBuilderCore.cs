using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace BrandUp.Worker.Builder
{
    public class WorkerBuilderCore : IWorkerBuilderCore
    {
        private readonly TaskMetadataManager taskMetadataManager = new TaskMetadataManager();

        public WorkerBuilderCore(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));

            AddCoreServices(services);
        }

        public ITaskMetadataManager TaskMetadataManager => taskMetadataManager;
        public IServiceCollection Services { get; }

        private void AddCoreServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(ITaskTypeResolver), this);
            services.AddSingleton<ITaskMetadataManager>(taskMetadataManager);
        }

        public IWorkerBuilderCore AddTaskAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var assemblyTypes = assembly.GetTypes();
            foreach (var type in assemblyTypes)
            {
                if (!TaskMetadata.CheckTaskType(type))
                    continue;

                taskMetadataManager.AddTaskType(type);
            }

            return this;
        }
        public IWorkerBuilderCore AddTaskType(Type taskType)
        {
            taskMetadataManager.AddTaskType(taskType);

            return this;
        }
    }

    public interface IWorkerBuilderCore
    {
        ITaskMetadataManager TaskMetadataManager { get; }
        IServiceCollection Services { get; }
        IWorkerBuilderCore AddTaskAssembly(Assembly assembly);
        IWorkerBuilderCore AddTaskType(Type taskType);
    }

    public class WorkerOptions
    {

    }
}