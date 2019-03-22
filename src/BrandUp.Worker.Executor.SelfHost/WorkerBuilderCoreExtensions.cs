using BrandUp.Worker.Allocator;
using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderCoreExtensions
    {
        public static IWorkerExecutorBuilder AddLocalExecutor(this IWorkerBuilderCore builder)
        {
            builder.Services.AddSingleton<ITaskAllocator, LocalTaskAllocator>();
            return builder.AddExecutor();
        }
    }
}