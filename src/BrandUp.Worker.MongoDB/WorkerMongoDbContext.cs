using BrandUp.MongoDB;
using MongoDB.Driver;

namespace BrandUp.Worker.MongoDB
{
    public class WorkerMongoDbContext : MongoDbContext, IWorkerMongoDbContext
    {
        public IMongoCollection<Tasks.TaskDocument> Tasks => GetCollection<Tasks.TaskDocument>();

        public WorkerMongoDbContext(MongoDbContextOptions options) : base(options) { }
    }

    public interface IWorkerMongoDbContext
    {
        IMongoCollection<Tasks.TaskDocument> Tasks { get; }
    }
}