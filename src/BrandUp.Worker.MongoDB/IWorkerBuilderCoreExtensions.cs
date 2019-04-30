using BrandUp.MongoDB;
using BrandUp.Worker.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BrandUp.Worker.Builder
{
    public static class IWorkerBuilderCoreExtensions
    {
        public static IWorkerBuilderCore AddMongoDb(this IWorkerBuilderCore builder, Action<IMongoDbContextBuilder> optionsAction)
        {
            builder.Services.AddMongoDbContext<MongoDB.WorkerMongoDbContext>(optionsAction);

            AddMongoDb<MongoDB.WorkerMongoDbContext>(builder);

            return builder;
        }

        public static IWorkerBuilderCore AddMongoDb(this IWorkerBuilderCore builder, IConfiguration configuration)
        {
            builder.Services.AddMongoDbContext<MongoDB.WorkerMongoDbContext>(configuration);

            AddMongoDb<MongoDB.WorkerMongoDbContext>(builder);

            return builder;
        }

        public static IWorkerBuilderCore AddMongoDb<TContext>(this IWorkerBuilderCore builder)
            where TContext : MongoDbContext, MongoDB.IWorkerMongoDbContext
        {
            builder.Services.AddMongoDbContextExension<TContext, MongoDB.IWorkerMongoDbContext>();

            builder.Services.AddSingleton<ITaskRepository, MongoDbTaskRepository>();

            return builder;
        }
    }
}