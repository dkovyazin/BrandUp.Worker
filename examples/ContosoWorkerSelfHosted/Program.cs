﻿using BrandUp.Worker;
using BrandUp.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ContosoWorker.SelfHosted
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory());
                    configApp.AddJsonFile("hostsettings.json", optional: true);
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    //configApp.AddEnvironmentVariables(prefix: "PREFIX_");
                    configApp.AddCommandLine(args);
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddWorkerAllocator(options =>
                        {
                            options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
                        })
                        .AddTaskType<Tasks.TestTask>()
                        .AddLocalExecutor()
                        .MapTaskHandler<Tasks.TestTask, Handlers.TestTaskHandler>();
                })
                .UseConsoleLifetime()
                .Build();

            using (host)
            {
                await host.StartAsync();

                while (true)
                {
                    var v = Console.ReadLine();
                    if (string.IsNullOrEmpty(v))
                    {
                        await host.StopAsync();
                        break;
                    }
                    else
                    {
                        using (var scope = host.Services.CreateScope())
                        {
                            var tasks = scope.ServiceProvider.GetRequiredService<ITaskService>();

                            var taskId = await tasks.PushTaskAsync(new Tasks.TestTask());
                            Console.WriteLine($"Add task {taskId}");
                        }
                    }
                }
            }
        }
    }
}