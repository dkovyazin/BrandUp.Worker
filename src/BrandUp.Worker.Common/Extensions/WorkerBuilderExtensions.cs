namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerBuilderCore AddTaskType<TTask>(this IWorkerBuilderCore builder)
        {
            builder.AddTaskType(typeof(TTask));
            return builder;
        }
    }
}