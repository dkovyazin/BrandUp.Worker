using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.Worker.Builder
{
    public static class IWorkerBuilderCoreExtensions
    {
        public static IWorkerBuilderCore AddTaskRepository<T>(this IWorkerBuilderCore builder)
            where T : class, Tasks.ITaskRepository
        {
            builder.Services.AddSingleton<Tasks.ITaskRepository, T>();

            return builder;
        }
    }
}