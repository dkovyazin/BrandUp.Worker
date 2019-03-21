using BrandUp.Worker;
using BrandUp.Worker.Allocator;
using BrandUp.Worker.Builder;
using BrandUp.Worker.Tasks;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IWorkerBuilderCore AddWorkerAllocator(this IServiceCollection services)
        {
            return services.AddWorkerAllocator(options => { });
        }

        public static IWorkerBuilderCore AddWorkerAllocator(this IServiceCollection services, Action<TaskAllocatorOptions> setupAction)
        {
            var builder = services.AddWorkerCore();

            builder.Services.Configure(setupAction);
            builder.Services.AddSingleton<ITaskRepository, DefaultTaskRepository>();
            builder.Services.AddSingleton<ITaskAllocator, TaskAllocator>();
            builder.Services.AddSingleton<ITaskService, LocalTaskService>();

            return builder;
        }
    }
}