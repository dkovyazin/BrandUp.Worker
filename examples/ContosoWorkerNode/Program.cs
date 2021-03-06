﻿using BrandUp.Worker;
using BrandUp.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ContosoWorker.Node
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder();

            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environmentName != null)
                hostBuilder.UseEnvironment(environmentName);

            hostBuilder.ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory());
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                    configApp.AddCommandLine(args);
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var executorBuilder = services.AddWorkerExecutor(new Uri("https://localhost:44338/"));

                    executorBuilder
                        .AddTaskType<Tasks.TestTask>();

                    executorBuilder
                        .MapTaskHandler<Tasks.TestTask, Handlers.TestTaskHandler>();
                })
                .UseConsoleLifetime();

            var host = hostBuilder.Build();

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

            Console.ReadLine();
        }
    }
}