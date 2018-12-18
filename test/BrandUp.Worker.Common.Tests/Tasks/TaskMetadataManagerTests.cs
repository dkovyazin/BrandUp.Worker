using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace BrandUp.Worker.Tasks
{
    public class TaskMetadataManagerTests : IDisposable
    {
        private readonly ServiceProvider serviceProvider;
        private readonly IServiceScope serviceScope;
        private readonly ITaskMetadataManager metadataManager;

        public TaskMetadataManagerTests()
        {
            var services = new ServiceCollection();
            services.AddWorkerCore()
                .AddTaskType(typeof(TestTask));

            serviceProvider = services.BuildServiceProvider();
            serviceScope = serviceProvider.CreateScope();

            metadataManager = serviceScope.ServiceProvider.GetService<ITaskMetadataManager>();
        }

        void IDisposable.Dispose()
        {
            serviceScope.Dispose();
            serviceProvider.Dispose();
        }

        [Fact]
        public void HasTaskType()
        {
            Assert.True(metadataManager.HasTaskType(typeof(TestTask)));
        }

        [Fact]
        public void FindTaskMetadataByObject()
        {
            var task = new TestTask();

            var taskMetadata = metadataManager.FindTaskMetadata(task);

            Assert.NotNull(taskMetadata);
            Assert.Equal(task.GetType(), taskMetadata.TaskType);
        }

        [Fact]
        public void FindTaskMetadataByType()
        {
            var task = new TestTask();

            var taskMetadata = metadataManager.FindTaskMetadata(task.GetType());

            Assert.NotNull(taskMetadata);
            Assert.Equal(task.GetType(), taskMetadata.TaskType);
        }

        [Fact]
        public void FindTaskMetadataByName()
        {
            var taskMetadata = metadataManager.FindTaskMetadata("Test");

            Assert.NotNull(taskMetadata);
            Assert.Equal("Test", taskMetadata.TaskName);
        }

        [Fact]
        public void Tasks_Contains()
        {
            var task = new TestTask();

            var taskMetadata = metadataManager.FindTaskMetadata(task);

            Assert.Contains(taskMetadata, metadataManager.Tasks);
        }
    }

    [Task]
    public class TestTask
    {
    }
}