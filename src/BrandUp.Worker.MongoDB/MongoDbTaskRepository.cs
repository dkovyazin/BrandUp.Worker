using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    public class MongoDbTaskRepository : ITaskRepository
    {
        private readonly ITaskMetadataManager taskMetadataManager;
        private MongoClient client;
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<TaskDocument> collection;

        public MongoDbTaskRepository(IOptions<MongoDbOptions> options, ITaskMetadataManager taskMetadataManager)
        {
            this.taskMetadataManager = taskMetadataManager ?? throw new ArgumentNullException(nameof(taskMetadataManager));
            client = new MongoClient(options.Value.ConnectionString);
            database = client.GetDatabase(options.Value.DatabaseName);

            collection = database.GetCollection<TaskDocument>(options.Value.CollectionName);
        }

        public async Task<IEnumerable<TaskState>> GetActualTasksAsync(CancellationToken cancellationToken = default)
        {
            var tasks = new List<TaskState>();

            var cursor = await collection.FindAsync(it => it.IsFinished == false, cancellationToken: cancellationToken);
            foreach (var doc in cursor.ToEnumerable(cancellationToken))
            {
                var taskMetadata = taskMetadataManager.FindTaskMetadata(doc.TypeName);
                if (taskMetadata == null)
                    throw new Exception();

                var jObj = JObject.Parse(doc.Model.ToString());

                var taskState = new TaskState
                {
                    TaskId = doc.Id,
                    CreatedDate = doc.CreatedDate,
                    TaskModel = jObj.ToObject(taskMetadata.TaskType),
                    EndDate = doc.EndDate
                };

                if (doc.Execution != null)
                {
                    taskState.ExecutorId = doc.Execution.ExecutorId;
                    taskState.StartedDate = doc.Execution.StartedDate;
                }

                tasks.Add(taskState);
            }

            return tasks;
        }

        public async Task PushTaskAsync(Guid taskId, string taskTypeName, object taskModel, DateTime createdDate, CancellationToken cancellationToken = default)
        {
            var jObj = JObject.FromObject(taskModel);
            var modelDoc = BsonDocument.Parse(jObj.ToString());

            var doc = new TaskDocument
            {
                Id = taskId,
                CreatedDate = createdDate,
                TypeName = taskTypeName,
                Model = modelDoc
            };

            await collection.InsertOneAsync(doc, new InsertOneOptions { BypassDocumentValidation = false }, cancellationToken);
        }

        public async Task TaskStartedAsync(Guid taskId, Guid executorId, DateTime startedDate, CancellationToken cancellationToken = default)
        {
            var docUpdate = Builders<TaskDocument>.Update.Set(it => it.Execution, new TaskExecution
            {
                Status = TaskExecutionStatus.Started,
                StartedDate = startedDate,
                ExecutorId = executorId
            });

            var doc = await collection.FindOneAndUpdateAsync(it => it.Id == taskId, docUpdate, cancellationToken: cancellationToken);
            if (doc == null)
                throw new Exception();
        }

        public async Task TaskSuccessAsync(Guid taskId, TimeSpan executingTime, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var docUpdate = Builders<TaskDocument>.Update
                .Set(it => it.EndDate, endDate)
                .Set(it => it.Execution.Status, TaskExecutionStatus.Success)
                .Set(it => it.Execution.ExecutionTime, executingTime)
                .Set(it => it.IsFinished, true);

            var doc = await collection.FindOneAndUpdateAsync(it => it.Id == taskId, docUpdate, cancellationToken: cancellationToken);
            if (doc == null)
                throw new Exception();
        }

        public async Task TaskErrorAsync(Guid taskId, TimeSpan executingTime, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var docUpdate = Builders<TaskDocument>.Update
                .Set(it => it.EndDate, endDate)
                .Set(it => it.Execution.Status, TaskExecutionStatus.Error)
                .Set(it => it.Execution.ExecutionTime, executingTime)
                .Set(it => it.IsFinished, true);

            var doc = await collection.FindOneAndUpdateAsync(it => it.Id == taskId, docUpdate, cancellationToken: cancellationToken);
            if (doc == null)
                throw new Exception();
        }

        public async Task TaskCancelledAsync(Guid taskId, DateTime endDate, string reason, CancellationToken cancellationToken = default)
        {
            var docUpdate = Builders<TaskDocument>.Update
                .Set(it => it.EndDate, endDate)
                .Set(it => it.IsFinished, true);

            var doc = await collection.FindOneAndUpdateAsync(it => it.Id == taskId, docUpdate, cancellationToken: cancellationToken);
            if (doc == null)
                throw new Exception();
        }

        public async Task TaskDeferedAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var docUpdate = Builders<TaskDocument>.Update
                .Set(it => it.Execution, null);

            var doc = await collection.FindOneAndUpdateAsync(it => it.Id == taskId, docUpdate, cancellationToken: cancellationToken);
            if (doc == null)
                throw new Exception();
        }
    }

    public class MongoDbOptions
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; } = "Tasks";
    }

    public class TaskDocument
    {
        [BsonId, BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedDate { get; set; }
        public string TypeName { get; set; }
        public BsonDocument Model { get; set; }
        public TaskExecution Execution { get; set; }
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? EndDate { get; set; }
        public bool IsFinished { get; set; }
    }

    public class TaskExecution
    {
        public TaskExecutionStatus Status { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public Guid ExecutorId { get; set; }
        [BsonRepresentation(MongoDB.Bson.BsonType.DateTime)]
        public DateTime StartedDate { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    public enum TaskExecutionStatus
    {
        Started,
        Success,
        Error
    }
}