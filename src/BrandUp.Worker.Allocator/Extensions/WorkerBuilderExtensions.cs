using BrandUp.Worker.Allocator;
using BrandUp.Worker.Allocator.Infrastructure;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerBuilder AddAllocator(this IWorkerBuilder builder)
        {
            return builder.AddAllocator(options => { });
        }

        public static IWorkerBuilder AddAllocator(this IWorkerBuilder builder, Action<TaskAllocatorOptions> setupAction)
        {
            builder.Services.Configure(setupAction);
            builder.Services.AddSingleton<ITaskAllocator, TaskAllocator>();
            builder.Services.AddSingleton<ITaskService, LocalTaskService>();
            return builder;
        }
    }
}