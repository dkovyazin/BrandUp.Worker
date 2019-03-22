using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    public class MemoryTaskRepository : ITaskRepository
    {
        private readonly ConcurrentDictionary<Guid, MemoryTaskEntity> tasks = new ConcurrentDictionary<Guid, MemoryTaskEntity>();

        public IEnumerable<Guid> TaskIds => tasks.Keys;
        public IEnumerable<MemoryTaskEntity> Tasks => tasks.Values;

        public Task<IEnumerable<TaskState>> GetActualTasksAsync(CancellationToken cancellationToken = default)
        {
            var taskStates = new List<TaskState>();
            foreach (var task in tasks.Values.Where(it => (it.Execution == null) || (it.Execution.Status == TaskExecutionStatus.Started)))
            {
                var taskState = new TaskState
                {
                    TaskId = task.TaskId,
                    TaskModel = task.TaskModel,
                    CreatedDate = task.CreatedDate,
                    EndDate = task.EndDate
                };

                if (task.Execution != null)
                {
                    taskState.ExecutorId = task.Execution.ExecutorId;
                    taskState.StartedDate = task.Execution.StartDate;
                }

                taskStates.Add(taskState);
            }
            return Task.FromResult<IEnumerable<TaskState>>(taskStates);
        }
        public Task PushTaskAsync(Guid taskId, string taskTypeName, object taskModel, DateTime createdDate, CancellationToken cancellationToken = default)
        {
            if (!tasks.TryAdd(taskId, new MemoryTaskEntity(taskId, createdDate, taskTypeName, taskModel)))
                throw new InvalidOperationException();
            return Task.CompletedTask;
        }
        public Task TaskStartedAsync(Guid taskId, Guid executorId, DateTime startedDate, CancellationToken cancellationToken = default)
        {
            if (!tasks.TryGetValue(taskId, out MemoryTaskEntity task))
                throw new InvalidOperationException();

            task.StartTask(executorId, startedDate);

            return Task.CompletedTask;
        }
        public Task TaskDeferedAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            if (!tasks.TryGetValue(taskId, out MemoryTaskEntity task))
                throw new InvalidOperationException();

            task.DeferTask();

            return Task.CompletedTask;
        }
        public Task TaskSuccessAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate, CancellationToken cancellationToken = default)
        {
            if (!tasks.TryGetValue(taskId, out MemoryTaskEntity task))
                throw new InvalidOperationException();

            task.SuccessTask(executingTime, doneDate);

            return Task.CompletedTask;
        }
        public Task TaskErrorAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate, CancellationToken cancellationToken = default)
        {
            if (!tasks.TryGetValue(taskId, out MemoryTaskEntity task))
                throw new InvalidOperationException();

            task.ErrorTask(executingTime, doneDate);

            return Task.CompletedTask;
        }
        public Task TaskCancelledAsync(Guid taskId, DateTime doneDate, string reason, CancellationToken cancellationToken = default)
        {
            if (!tasks.TryGetValue(taskId, out MemoryTaskEntity task))
                throw new InvalidOperationException();

            task.CancelTask(doneDate);

            return Task.CompletedTask;
        }
    }

    public class MemoryTaskEntity
    {
        public Guid TaskId { get; }
        public object TaskModel { get; }
        public DateTime CreatedDate { get; }
        public string TypeName { get; }
        public DateTime? EndDate { get; private set; }
        public MemoryTaskExecutionEntity Execution { get; private set; }

        public MemoryTaskEntity(Guid taskId, DateTime createdDate, string taskTypeName, object taskModel)
        {
            TaskId = taskId;
            CreatedDate = createdDate;
            TypeName = taskTypeName;
            TaskModel = taskModel;
        }

        public void StartTask(Guid executorId, DateTime startDate)
        {
            if (Execution != null)
                throw new InvalidOperationException("Задача уже выполняется.");

            Execution = new MemoryTaskExecutionEntity(executorId, startDate);
        }
        public void DeferTask()
        {
            if (Execution == null)
                throw new InvalidOperationException("Задача уже не выполняется.");

            Execution = null;
        }
        public void SuccessTask(TimeSpan executingTime, DateTime finishDate)
        {
            EndDate = finishDate;
            Execution.ExecutionTime = executingTime;
            Execution.Status = TaskExecutionStatus.Success;
        }
        public void ErrorTask(TimeSpan executingTime, DateTime finishDate)
        {
            EndDate = finishDate;
            Execution.ExecutionTime = executingTime;
            Execution.Status = TaskExecutionStatus.Error;
        }
        public void CancelTask(DateTime finishDate)
        {
            EndDate = finishDate;
        }
    }
    public class MemoryTaskExecutionEntity
    {
        public Guid ExecutorId { get; }
        public DateTime StartDate { get; }
        public TimeSpan? ExecutionTime { get; set; }
        public TaskExecutionStatus Status { get; set; }

        public MemoryTaskExecutionEntity(Guid executorId, DateTime startDate)
        {
            Status = TaskExecutionStatus.Started;
            ExecutorId = executorId;
            StartDate = startDate;
        }
    }
    public enum TaskExecutionStatus
    {
        Started,
        Success,
        Error
    }
}