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

    public class AssemblyCommandTypeResolver : ITaskTypeResolver
    {
        private readonly List<Assembly> _assemblies;

        public AssemblyCommandTypeResolver(params Assembly[] assemblies) : this((IEnumerable<Assembly>)assemblies) { }
        public AssemblyCommandTypeResolver(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            _assemblies = assemblies.ToList();
        }

        public IEnumerable<Type> GetCommandTypes()
        {
            foreach (var assembly in _assemblies)
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
    }
}