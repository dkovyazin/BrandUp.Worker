using BrandUp.MongoDB;
using MongoDB.Driver;

namespace BrandUp.Worker.MongoDB
{
    public class WorkerMongoDbDbContext : MongoDbContext, IWorkerMongoDbDbContext
    {
        public IMongoCollection<Tasks.TaskDocument> Tasks => GetCollection<Tasks.TaskDocument>();

        public WorkerMongoDbDbContext(MongoDbContextOptions options) : base(options) { }
    }

    public interface IWorkerMongoDbDbContext
    {
        IMongoCollection<Tasks.TaskDocument> Tasks { get; }
    }
}