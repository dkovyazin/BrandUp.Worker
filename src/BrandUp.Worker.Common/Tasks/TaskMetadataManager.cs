﻿using System;
using System.Collections.Generic;

namespace BrandUp.Worker.Tasks
{
    public class TaskMetadataManager : ITaskMetadataManager
    {
        private readonly List<TaskMetadata> tasks = new List<TaskMetadata>();
        private readonly Dictionary<Type, int> taskTypes = new Dictionary<Type, int>();
        private readonly Dictionary<string, int> taskNames = new Dictionary<string, int>();

        public TaskMetadataManager() { }

        public bool AddTaskType(Type taskType)
        {
            if (taskType == null)
                throw new ArgumentNullException(nameof(taskType));
            if (!TaskMetadata.CheckTaskType(taskType))
                throw new ArgumentException();

            if (taskTypes.ContainsKey(taskType))
                return false;

            var taskMetadata = new TaskMetadata(taskType);
            var index = tasks.Count;

            taskTypes.Add(taskType, index);
            taskNames.Add(taskMetadata.TaskName.ToLower(), index);
            tasks.Add(taskMetadata);

            return true;
        }

        #region ICommandMetadataManager members

        public IEnumerable<TaskMetadata> Tasks => tasks;
        public bool HasTaskType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return taskTypes.ContainsKey(type);
        }
        public TaskMetadata FindTaskMetadata(object taskModel)
        {
            if (taskModel == null)
                throw new ArgumentNullException(nameof(taskModel));

            return FindTaskMetadata(taskModel.GetType());
        }
        public TaskMetadata FindTaskMetadata<TTask>()
             where TTask : class, new()
        {
            return FindTaskMetadata(typeof(TTask));
        }
        public TaskMetadata FindTaskMetadata(string taskName)
        {
            if (taskName == null)
                throw new ArgumentNullException(nameof(taskName));

            if (!taskNames.TryGetValue(taskName.ToLower(), out int index))
                return null;

            return tasks[index];
        }
        public TaskMetadata FindTaskMetadata(Type taskType)
        {
            if (taskType == null)
                throw new ArgumentNullException(nameof(taskType));

            if (!taskTypes.TryGetValue(taskType, out int index))
                return null;

            return tasks[index];
        }

        #endregion
    }

    public interface ITaskMetadataManager
    {
        IEnumerable<TaskMetadata> Tasks { get; }
        bool HasTaskType(Type type);
        TaskMetadata FindTaskMetadata(object taskModel);
        TaskMetadata FindTaskMetadata<TTask>() where TTask : class, new();
        TaskMetadata FindTaskMetadata(string taskName);
        TaskMetadata FindTaskMetadata(Type taskType);
    }
}