using BrandUp.Worker.Allocator;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerExecutorBuilder AddExecutor(this IWorkerBuilderCore builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return new WorkerExecutorBuilder(builder);
        }

        public static IWorkerExecutorBuilder AddExecutorNode(this IWorkerBuilderCore builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddHttpClient("BrandUp.Worker.Allocator", (client) =>
            {
            });
            builder.Services.AddSingleton<ITaskAllocator, TaskAllocatorClient>();

            return new WorkerExecutorBuilder(builder);
        }
    }
}