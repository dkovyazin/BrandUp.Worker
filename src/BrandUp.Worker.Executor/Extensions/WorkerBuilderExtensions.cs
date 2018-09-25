using System;

namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerExecutorBuilder AddExecutor(this IWorkerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return new WorkerExecutorBuilder(builder);
        }
    }
}