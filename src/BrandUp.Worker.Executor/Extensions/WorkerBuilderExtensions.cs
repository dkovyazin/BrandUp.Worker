namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerExecutorBuilder AddExecutor(this IWorkerBuilderCore builder)
        {
            return new WorkerExecutorBuilder(builder);
        }
    }
}