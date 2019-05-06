# BrandUp.Worker

[![Build Status](https://dev.azure.com/brandup/BrandUp%20Core/_apis/build/status/BrandUp.Worker?branchName=master)](https://dev.azure.com/brandup/BrandUp%20Core/_build/latest?definitionId=14&branchName=master)

## Task definition

Tasks are defined using the attribute TaskAttribute.

Using NuGet package [BrandUp.Worker.Common](https://www.nuget.org/packages/BrandUp.Worker.Common/)

```
[Task(Name = "Custom name", StartTimeout = 100, ExecutionTimeout = 30000)]
public class TestTask
{
    // Task properties
}
```

## Task client startup
Using NuGet package [BrandUp.Worker.Client](https://www.nuget.org/packages/BrandUp.Worker.Client/)

```
services
    .AddWorkerClient(new System.Uri("https://localhost:44351/"))
    .AddTaskType<Tasks.TestTask>();
```

## Task service startup
Using NuGet package [BrandUp.Worker.Allocator.Host](https://www.nuget.org/packages/BrandUp.Worker.Allocator.Host/)

```
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddWorkerAllocator()
            .AddTaskType<Tasks.TestTask>();
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
```

## Worker executor startup

Using NuGet package [BrandUp.Worker.Executor.Host](https://www.nuget.org/packages/BrandUp.Worker.Executor.Host/)

```
var executorBuilder = services.AddWorkerExecutor(new Uri("https://localhost:44338/"));

executorBuilder
    .AddTaskType<Tasks.TestTask>();

executorBuilder
    .MapTaskHandler<Tasks.TestTask, Handlers.TestTaskHandler>();
```

## Worker self hosted startup

Using NuGet packages [BrandUp.Worker.SelfHost](https://www.nuget.org/packages/BrandUp.Worker.SelfHost/)

```
services
    .AddWorkerAllocator(options =>
    {
        options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
    })
    .AddTaskType<Tasks.TestTask>()
    .AddLocalExecutor()
    .MapTaskHandler<Tasks.TestTask, Handlers.TestTaskHandler>();
```
