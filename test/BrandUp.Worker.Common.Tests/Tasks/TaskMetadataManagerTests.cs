using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace BrandUp.Worker.Tasks
{
    public class TaskMetadataManagerTests : IDisposable
    {
        private readonly ServiceProvider serviceProvider;
        private readonly IServiceScope serviceScope;
        private readonly ITaskMetadataManager manager;

        public TaskMetadataManagerTests()
        {
            var services = new ServiceCollection();
            services.AddWorker()
                .AddTaskType(typeof(TestTask));

            serviceProvider = services.BuildServiceProvider();
            serviceScope = serviceProvider.CreateScope();

            manager = serviceScope.ServiceProvider.GetService<ITaskMetadataManager>();
        }

        void IDisposable.Dispose()
        {
            serviceScope.Dispose();
            serviceProvider.Dispose();
        }

        [Fact]
        public void HasTaskType()
        {
            Assert.True(manager.HasTaskType(typeof(TestTask)));
        }

        [Fact]
        public void FindTaskMetadataByObject()
        {
            var task = new TestTask();

            var taskMetadata = manager.FindTaskMetadata(task);

            Assert.NotNull(taskMetadata);
            Assert.Equal(task.GetType(), taskMetadata.TaskType);
        }

        [Fact]
        public void FindTaskMetadataByType()
        {
            var task = new TestTask();

            var taskMetadata = manager.FindTaskMetadata(task.GetType());

            Assert.NotNull(taskMetadata);
            Assert.Equal(task.GetType(), taskMetadata.TaskType);
        }

        [Fact]
        public void FindTaskMetadataByName()
        {
            var taskMetadata = manager.FindTaskMetadata("Test");

            Assert.NotNull(taskMetadata);
            Assert.Equal("Test", taskMetadata.TaskName);
        }

        [Fact]
        public void Tasks_Contains()
        {
            var task = new TestTask();

            var taskMetadata = manager.FindTaskMetadata(task);

            Assert.Contains(taskMetadata, manager.Tasks);
        }
    }

    [Task]
    public class TestTask
    {
    }
}