using Microsoft.Extensions.DependencyInjection;

namespace BrandUp.Worker.Builder
{
    public class WorkerAllocatorBuilder : WorkerBuilderCore
    {
        public WorkerAllocatorBuilder(IServiceCollection services) : base(services)
        {

        }
    }
}