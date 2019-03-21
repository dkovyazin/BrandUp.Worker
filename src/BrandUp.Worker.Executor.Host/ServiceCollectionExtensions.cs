using BrandUp.Worker.Allocator;
using BrandUp.Worker.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IWorkerExecutorBuilder AddWorkerExecutorHost(this IServiceCollection services, Uri workerServiceUrl)
        {
            if (workerServiceUrl == null)
                throw new ArgumentNullException(nameof(workerServiceUrl));

            var builder = services.AddWorkerCore();

            services.AddTasksServiceClient(workerServiceUrl);

            services.AddHttpClient<ITaskAllocator, RemoteTaskAllocator>((options) =>
            {
                options.BaseAddress = workerServiceUrl;
            });

            return builder.AddExecutor();
        }
    }
}