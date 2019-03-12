namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerExecutorBuilder AddExecutor(this IWorkerBuilderCore builder)
        {
            var executorBuilder = new WorkerExecutorBuilder(builder);
            return executorBuilder;
        }
    }
}