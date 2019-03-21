using BrandUp.Worker.Tasks;
using System;
using System.Reflection;

namespace BrandUp.Worker.Builder
{
    public static class WorkerBuilderExtensions
    {
        public static IWorkerBuilderCore AddTaskType<TTask>(this IWorkerBuilderCore builder)
        {
            builder.AddTaskType(typeof(TTask));
            return builder;
        }

        public static IWorkerBuilderCore AddTaskAssembly(this IWorkerBuilderCore builder, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var assemblyTypes = assembly.GetTypes();
            foreach (var type in assemblyTypes)
            {
                if (!TaskMetadata.CheckTaskType(type))
                    continue;

                builder.AddTaskType(type);
            }

            return builder;
        }
    }
}