using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace BrandUp.Worker.Builder
{
    public class WorkerBuilder : IWorkerBuilder
    {
        private readonly TaskMetadataManager taskMetadataManager = new TaskMetadataManager();

        public WorkerBuilder(IServiceCollection services)
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

        public IWorkerBuilder AddTaskAssembly(Assembly assembly)
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
        public IWorkerBuilder AddTaskType(Type taskType)
        {
            taskMetadataManager.AddTaskType(taskType);

            return this;
        }
    }

    public interface IWorkerBuilder
    {
        ITaskMetadataManager TaskMetadataManager { get; }
        IServiceCollection Services { get; }
        IWorkerBuilder AddTaskAssembly(Assembly assembly);
        IWorkerBuilder AddTaskType(Type taskType);
    }

    public class WorkerOptions
    {

    }
}
