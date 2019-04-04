using System;

namespace BrandUp.Worker
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TaskAttribute : Attribute
    {
        public string Name { get; set; }
        public int StartTimeout { get; set; } = 0;
        public int ExecutionTimeout { get; set; } = 30000;
    }
}