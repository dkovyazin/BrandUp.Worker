using System;

namespace BrandUp.Worker.Tasks
{
    public static class TaskMetadataManagerExtensions
    {
        public static bool HasTaskType<TTask>(this ITaskMetadataManager taskMetadataManager)
        {
            if (taskMetadataManager == null)
                throw new ArgumentNullException(nameof(taskMetadataManager));

            return taskMetadataManager.HasTaskType(typeof(TTask));
        }
    }
}