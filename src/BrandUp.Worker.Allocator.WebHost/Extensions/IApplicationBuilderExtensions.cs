using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BrandUp.Worker.Builder
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAllocatorHost(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Map(new PathString("/brandup.worker"), (builder) =>
            {
                builder.UseMiddleware<WorkerClientMiddleware>();
            });

            return applicationBuilder;
        }
    }

    internal class WorkerClientMiddleware
    {
        private readonly RequestDelegate _next;

        public WorkerClientMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var path = request.Path.Value.Trim(new char[] { '/' }).ToLower();

            response.StatusCode = 200;

            switch (path)
            {
                case "pushtask":
                    {
                        if (request.Method != "POST")
                        {
                            response.StatusCode = (int)System.Net.HttpStatusCode.MethodNotAllowed;
                            return Task.CompletedTask;
                        }

                        break;
                    }
            }

            return Task.CompletedTask;
        }
    }
}