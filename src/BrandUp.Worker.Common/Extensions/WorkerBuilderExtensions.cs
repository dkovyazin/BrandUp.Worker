namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerBuilder AddTaskType<TTask>(this IWorkerBuilder builder)
        {
            builder.AddTaskType(typeof(TTask));
            return builder;
        }
    }
}