using System;
using System.Collections.Generic;

namespace BrandUp.Worker.Executor
{
    public class TaskExecutorOptions
    {
        private readonly Dictionary<Type, Type> taskHandlerMappings = new Dictionary<Type, Type>();

        public IEnumerable<Type> TaskTypes => taskHandlerMappings.Keys;

        public void MapTaskHandler<TTask, THandler>()
            where TTask : class, new()
            where THandler : TaskHandler<TTask>
        {
            taskHandlerMappings.Add(typeof(TTask), typeof(THandler));
        }
    }
}