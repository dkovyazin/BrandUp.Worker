using BrandUp.Worker;
using BrandUp.Worker.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IWorkerBuilderCore AddWorkerClient(this IServiceCollection services, Uri workerServiceUrl)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (workerServiceUrl == null)
                throw new ArgumentNullException(nameof(workerServiceUrl));

            var builder = services.AddWorkerCore();

            services.AddHttpClient<ITaskService, RemoteTaskService>((options) =>
            {
                options.BaseAddress = workerServiceUrl;
            });

            return builder;
        }
    }
}