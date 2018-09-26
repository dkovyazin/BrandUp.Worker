using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    public class MemoryTaskRepository : ITaskRepository
    {
        private readonly ConcurrentDictionary<Guid, TaskEntity> tasks = new ConcurrentDictionary<Guid, TaskEntity>();

        public Task<IEnumerable<TaskState>> GetActualTasks()
        {
            var taskStates = new List<TaskState>();
            foreach (var task in tasks.Values)
            {
                var taskState = new TaskState
                {
                    TaskId = task.TaskId,
                    TaskModel = task.TaskModel,
                    CreatedDate = task.CreatedDate
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
        public Task PushTaskAsync(Guid taskId, object taskModel, DateTime createdDate)
        {
            if (!tasks.TryAdd(taskId, new TaskEntity(taskId, createdDate, taskModel)))
                throw new InvalidOperationException();
            return Task.CompletedTask;
        }
        public Task TaskStartedAsync(Guid taskId, Guid executorId, DateTime startedDate)
        {
            if (!tasks.TryGetValue(taskId, out TaskEntity task))
                throw new InvalidOperationException();

            task.StartTask(executorId, startedDate);

            return Task.CompletedTask;
        }
        public Task TaskDeferedAsync(Guid taskId)
        {
            if (!tasks.TryGetValue(taskId, out TaskEntity task))
                throw new InvalidOperationException();

            task.DeferTask();

            return Task.CompletedTask;
        }
        public Task TaskDoneAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate)
        {
            if (!tasks.TryRemove(taskId, out TaskEntity task))
                throw new InvalidOperationException();

            task.Execution.Done(executingTime, doneDate);

            return Task.CompletedTask;
        }
        public Task TaskErrorAsync(Guid taskId, TimeSpan executingTime, DateTime doneDate)
        {
            if (!tasks.TryRemove(taskId, out TaskEntity task))
                throw new InvalidOperationException();

            task.Execution.Error(executingTime, doneDate);

            return Task.CompletedTask;
        }
        public Task TaskCancelledAsync(Guid taskId, string reason)
        {
            if (!tasks.TryRemove(taskId, out TaskEntity task))
                throw new InvalidOperationException();

            return Task.CompletedTask;
        }

        private class TaskEntity
        {
            public Guid TaskId { get; }
            public object TaskModel { get; }
            public DateTime CreatedDate { get; }
            public TaskExecutionEntity Execution { get; private set; }

            public TaskEntity(Guid taskId, DateTime createdDate, object taskModel)
            {
                TaskId = taskId;
                CreatedDate = createdDate;
                TaskModel = taskModel;
            }

            public void StartTask(Guid executorId, DateTime startDate)
            {
                if (Execution != null)
                    throw new InvalidOperationException("Задача уже выполняется.");

                Execution = new TaskExecutionEntity(executorId, startDate);
            }

            public void DeferTask()
            {
                if (Execution == null)
                    throw new InvalidOperationException("Задача уже не выполняется.");

                Execution = null;
            }
        }
        private class TaskExecutionEntity
        {
            public Guid ExecutorId { get; }
            public DateTime StartDate { get; }
            public TimeSpan? ExecutionTime { get; private set; }
            public DateTime FinishDate { get; private set; }

            public TaskExecutionEntity(Guid executorId, DateTime startDate)
            {
                ExecutorId = executorId;
                StartDate = startDate;
            }

            public void Done(TimeSpan executingTime, DateTime doneDate)
            {
                ExecutionTime = executingTime;
                FinishDate = doneDate;
            }
            public void Error(TimeSpan executingTime, DateTime doneDate)
            {
                ExecutionTime = executingTime;
                FinishDate = doneDate;
            }
        }
    }
}