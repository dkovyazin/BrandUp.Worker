using BrandUp.Worker.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IWorkerBuilderCore AddWorkerCore(this IServiceCollection services)
        {
            return AddWorkerCore(services, (options) => { });
        }

        public static IWorkerBuilderCore AddWorkerCore(this IServiceCollection services, Action<WorkerOptions> setupAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.Configure(setupAction);

            return new WorkerBuilderCore(services);
        }
    }
}