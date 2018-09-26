using System;
using System.Reflection;

namespace BrandUp.Worker.Tasks
{
    public class TaskMetadata
    {
        private const string TaskNamePrefix = "Task";

        public string TaskName { get; }
        public Type TaskType { get; }
        public int TimeoutWaitingToStartInMiliseconds { get; }
        public int ExecutionTimeout { get; }

        public TaskMetadata(Type objectType)
        {
            TaskType = objectType ?? throw new ArgumentNullException(nameof(objectType));
            if (objectType.IsAbstract)
                throw new ArgumentException();
            if (!CheckTaskType(objectType))
                throw new ArgumentException();

            var taskAttribute = objectType.GetCustomAttribute<TaskAttribute>(false);
            if (taskAttribute == null)
                throw new ArgumentException();

            var taskName = taskAttribute.Name;
            if (string.IsNullOrEmpty(taskName))
            {
                taskName = objectType.Name;

                if (taskName.EndsWith(TaskNamePrefix))
                    taskName = taskName.Substring(0, taskName.Length - TaskNamePrefix.Length);
            }
            TaskName = taskName;
            TimeoutWaitingToStartInMiliseconds = taskAttribute.TimeoutWaitingToStartInMiliseconds;
            ExecutionTimeout = taskAttribute.ExecutionTimeout;
        }

        public static bool CheckTaskType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.IsDefined(typeof(TaskAttribute), false);
        }
    }
}