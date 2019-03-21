using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;

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

        private void AddCoreServices(IServiceCollection services)
        {
            services.AddSingleton<ITaskMetadataManager>(taskMetadataManager);
            services.AddSingleton<Remoting.IContractSerializer, Remoting.JsonContractSerializer>();
        }

        #region IWorkerBuilderCore members

        public IServiceCollection Services { get; }
        public ITaskMetadataManager TasksMetadata => taskMetadataManager;
        public IWorkerBuilderCore AddTaskType(Type taskType)
        {
            taskMetadataManager.AddTaskType(taskType);

            return this;
        }

        #endregion
    }

    public interface IWorkerBuilderCore
    {
        IServiceCollection Services { get; }
        ITaskMetadataManager TasksMetadata { get; }
        IWorkerBuilderCore AddTaskType(Type taskType);
    }
}