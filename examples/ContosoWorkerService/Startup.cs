using BrandUp.Worker.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ContosoWorker.Service
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
            var workerBuilder = services
                .AddWorkerAllocator(options =>
                {
                    options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
                });

            workerBuilder.AddTaskType<Tasks.TestTask>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseHttpsRedirection();
            app.UseWorkerAllocator();
        }
    }
}