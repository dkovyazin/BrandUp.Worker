using BrandUp.MongoDB;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BrandUp.Worker.Builder
{
    public static class IWorkerBuilderCoreExtensions
    {
        public static IWorkerBuilderCore AddMongoDb(this IWorkerBuilderCore builder, Action<IMongoDbContextBuilder> optionsAction)
        {
            builder.Services.AddMongoDbContext<MongoDB.WorkerMongoDbDbContext>(optionsAction);
            builder.Services.AddMongoDbContextExension<MongoDB.WorkerMongoDbDbContext, MongoDB.IWorkerMongoDbDbContext>();

            builder.Services.AddSingleton<ITaskRepository, MongoDbTaskRepository>();

            return builder;
        }

        public static IWorkerBuilderCore AddMongoDb<TContext>(this IWorkerBuilderCore builder)
            where TContext : MongoDbContext, MongoDB.IWorkerMongoDbDbContext
        {
            builder.Services.AddMongoDbContextExension<TContext, MongoDB.IWorkerMongoDbDbContext>();

            builder.Services.AddSingleton<ITaskRepository, MongoDbTaskRepository>();

            return builder;
        }
    }
}