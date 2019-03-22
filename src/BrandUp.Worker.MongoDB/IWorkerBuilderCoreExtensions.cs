using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BrandUp.Worker.Builder
{
    public static class IWorkerBuilderCoreExtensions
    {
        public static IWorkerBuilderCore AddMongoDbRepository(this IWorkerBuilderCore builder, Action<MongoDbOptions> mongoDbOptions)
        {
            MongoDbConfig.EnsureConfigured();

            builder.Services.Configure(mongoDbOptions);
            builder.Services.AddSingleton<ITaskRepository, MongoDbTaskRepository>();

            return builder;
        }
    }
}