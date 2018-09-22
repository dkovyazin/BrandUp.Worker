using Xunit;

namespace BrandUp.Worker.Tasks
{
    public class TaskMetadataManagerTests
    {
        private readonly TaskMetadataManager manager;

        public TaskMetadataManagerTests()
        {
            manager = new TaskMetadataManager(new AssemblyCommandTypeResolver(typeof(TestTask).Assembly));
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