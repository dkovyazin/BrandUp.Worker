using BrandUp.Worker.Builder;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IWorkerBuilderCore AddWorkerCore(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return new WorkerBuilderCore(services);
        }
    }
}