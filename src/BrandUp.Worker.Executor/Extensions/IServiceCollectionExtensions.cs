using BrandUp.Worker.Executor;

namespace Microsoft.Extensions.DependencyInjection
{
    internal static class IServiceCollectionExtensions
    {
        public static void AddTaskHandler<TTask, THandler>(this IServiceCollection services)
            where TTask : class, new()
            where THandler : TaskHandler<TTask>
        {
            services.AddTransient<TaskHandler<TTask>, THandler>();
        }
    }
}