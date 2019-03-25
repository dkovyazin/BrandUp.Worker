using BrandUp.Worker.Allocator;
using BrandUp.Worker.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IWorkerExecutorBuilder AddWorkerExecutor(this IServiceCollection services, Uri workerServiceUrl)
        {
            if (workerServiceUrl == null)
                throw new ArgumentNullException(nameof(workerServiceUrl));

            var builder = services.AddWorkerClient(workerServiceUrl);

            services.AddHttpClient<WorkerServiceClient>((options) =>
            {
                options.BaseAddress = workerServiceUrl;
            });

            services.AddScoped<ITaskAllocator, RemoteTaskAllocator>();

            return builder.AddExecutor();
        }
    }
}