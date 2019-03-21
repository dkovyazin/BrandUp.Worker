# BrandUp.Worker

[![Build Status](https://dev.azure.com/brandup/BrandUp%20Core/_apis/build/status/BrandUp%20Worker?branchName=$/BrandUp%20Core/Main/Worker)](https://dev.azure.com/brandup/BrandUp%20Core/_build/latest?definitionId=3?branchName=$/BrandUp%20Core/Main/Worker)

## Task definition
Tasks are defined using the attribute TaskAttribute.
```
[Task(Name = "Custom name", TimeoutWaitingToStartInMiliseconds = 100, ExecutionTimeout = 30000)]
public class TestTask
{
    // Task properties
}
```

## Worker service startup
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
```
var executorBuilder = services.AddWorkerExecutorHost(new Uri("https://localhost:44338/"));

executorBuilder
    .AddTaskType<Tasks.TestTask>();

executorBuilder
    .MapTaskHandler<Tasks.TestTask, Handlers.TestTaskHandler>();
```

## Worker self hosted startup
```
services
    .AddWorkerAllocator(options =>
    {
        options.TimeoutWaitingTasksPerExecutor = TimeSpan.FromSeconds(2);
    })
    .AddTaskType<Tasks.TestTask>()
    .AddExecutor()
    .MapTaskHandler<Tasks.TestTask, Handlers.TestTaskHandler>();
```
