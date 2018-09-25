using BrandUp.Worker.Tasks;
using System;
using Xunit;

namespace BrandUp.Worker.Allocator.Infrastructure
{
    public class TaskQueueTests
    {
        [Fact]
        public void HasTaskType()
        {
            var commandType = typeof(TestTask);
            var queue = new TaskQueue(new Type[] { commandType });

            Assert.Equal(1, queue.CountTaskTypes);
            Assert.True(queue.HasTaskType(commandType));
        }

        [Fact]
        public void Enqueue()
        {
            var commandType = typeof(TestTask);
            var queue = new TaskQueue(new Type[] { commandType });

            queue.Enqueue(new TaskContainer(Guid.NewGuid(), new TestTask()));

            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void Dequeue()
        {
            var commandType = typeof(TestTask);
            var queue = new TaskQueue(new Type[] { commandType });

            var taskId = Guid.NewGuid();
            var taskModel = new TestTask();
            queue.Enqueue(new TaskContainer(taskId, taskModel));

            var dequeueResult = queue.TryDequeue(commandType, out TaskContainer task);

            Assert.True(dequeueResult);
            Assert.Equal(taskModel, task.Task);
            Assert.Equal(taskId, task.TaskId);
            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void Dequeue_EmptyQueue()
        {
            var commandType = typeof(TestTask);
            var queue = new TaskQueue(new Type[] { commandType });

            var dequeueResult = queue.TryDequeue(commandType, out TaskContainer task);

            Assert.False(dequeueResult);
            Assert.Null(task);
        }
    }
}
