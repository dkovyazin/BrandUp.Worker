﻿using BrandUp.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ContosoWorker.SelfHosted
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory());
                    configApp.AddJsonFile("hostsettings.json", optional: true);
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    //configApp.AddEnvironmentVariables(prefix: "PREFIX_");
                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var workerBuilder = services.AddWorkerAllocator(options =>
                    {
                        options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
                    })
                        .AddTaskType<Tasks.TestTask>();

                    workerBuilder.AddExecutor()
                        .MapTaskHandler<Tasks.TestTask, Handlers.TestTaskHandler>();
                });

            await hostBuilder.RunConsoleAsync();
        }
    }
}