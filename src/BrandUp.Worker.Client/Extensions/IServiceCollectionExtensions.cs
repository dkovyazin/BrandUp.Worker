using BrandUp.Worker;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTasksServiceClient(this IServiceCollection services, Uri workerServiceUrl)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (workerServiceUrl == null)
                throw new ArgumentNullException(nameof(workerServiceUrl));

            services.AddHttpClient<ITaskService, RemoteTaskService>((options) =>
            {
                options.BaseAddress = workerServiceUrl;
            });

            return services;
        }
    }
}