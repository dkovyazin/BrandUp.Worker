using BrandUp.Worker.Allocator;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerBuilderCore AddAllocatorHost(this IWorkerBuilderCore builder)
        {
            return builder.AddAllocatorHost(options => { });
        }

        public static IWorkerBuilderCore AddAllocatorHost(this IWorkerBuilderCore builder, Action<TaskAllocatorOptions> setupAction)
        {
            builder.Services.Configure(setupAction);
            builder.Services.AddSingleton<ITaskRepository, DefaultTaskRepository>();
            builder.Services.AddSingleton<ITaskAllocator, TaskAllocator>();
            builder.Services.AddSingleton<ITaskService, LocalTaskService>();
            return builder;
        }
    }
}