using BrandUp.Worker.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BrandUp.Worker.Allocator
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWorkerCore()
                .AddTaskType(typeof(TestTask))
                .AddAllocatorHost(options =>
                {
                    options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
                });

            //services.AddMvcCore();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAllocatorHost();
        }
    }

    [Task(TimeoutWaitingToStartInMiliseconds = 100)]
    public class TestTask
    {
    }
}