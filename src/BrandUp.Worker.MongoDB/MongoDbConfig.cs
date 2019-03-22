using BrandUp.Worker.Tasks;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BrandUp.Worker
{
    internal class MongoDbConfig
    {
        private static bool _initialized = false;
        private static object _initializationLock = new object();
        private static object _initializationTarget;
        private static readonly List<Type> types = new List<Type>();

        public static void EnsureConfigured()
        {
            EnsureConfiguredImpl();
        }

        private static void EnsureConfiguredImpl()
        {
            LazyInitializer.EnsureInitialized(ref _initializationTarget, ref _initialized, ref _initializationLock, () =>
            {
                Configure();
                return null;
            });
        }

        private static void Configure()
        {
            types.Add(typeof(TaskDocument));
            types.Add(typeof(TaskExecution));

            RegisterConventions();
        }

        private static void RegisterConventions()
        {
            var pack = new ConventionPack
            {
                new IgnoreIfNullConvention(false),
                new CamelCaseElementNameConvention()
            };

            ConventionRegistry.Register("BrandUp.Worker.MongoDB", pack, IsConventionApplicable);
        }

        private static bool IsConventionApplicable(Type type)
        {
            return types.Contains(type);
        }
    }
}
