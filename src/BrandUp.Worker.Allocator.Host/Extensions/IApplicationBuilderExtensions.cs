using BrandUp.Worker.Allocator.WebHost;

namespace Microsoft.AspNetCore.Builder
{
    public static class IApplicationBuilderExtensions
    {
        public static void UseWorkerAllocator(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<WorkerMiddleware>();
        }
    }
}