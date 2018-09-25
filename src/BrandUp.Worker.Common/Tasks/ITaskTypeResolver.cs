using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BrandUp.Worker.Tasks
{
    public interface ITaskTypeResolver
    {
        IEnumerable<Type> GetCommandTypes();
    }

    public class AssemblyTaskTypeResolver : ITaskTypeResolver
    {
        private readonly List<Assembly> assemblies;

        public AssemblyTaskTypeResolver(params Assembly[] assemblies) : this((IEnumerable<Assembly>)assemblies) { }
        public AssemblyTaskTypeResolver(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            this.assemblies = assemblies.ToList();
        }

        public IEnumerable<Type> GetCommandTypes()
        {
            foreach (var assembly in assemblies)
            {
                var assemblyTypes = assembly.GetTypes();
                foreach (var assemblyType in assemblyTypes)
                {
                    if (!TaskMetadata.CheckTaskType(assemblyType))
                        continue;

                    yield return assemblyType;
                }
            }

            yield break;
        }

        public void AddAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (assemblies.Contains(assembly))
                return;

            assemblies.Add(assembly);
        }
    }
}