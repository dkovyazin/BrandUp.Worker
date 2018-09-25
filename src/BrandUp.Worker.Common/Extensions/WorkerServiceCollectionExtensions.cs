using BrandUp.Worker.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IWorkerBuilder AddWorker(this IServiceCollection services)
        {
            return AddWorker(services, (options) => { });
        }

        public static IWorkerBuilder AddWorker(this IServiceCollection services, Action<WorkerOptions> setupAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.Configure(setupAction);

            return new WorkerBuilder(services);
        }
    }
}