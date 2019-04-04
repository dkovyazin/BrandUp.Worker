using System;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocatorOptions
    {
        public string Name { get; set; } = "Default";
        public TimeSpan TimeoutWaitingTasksPerExecutor { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxTasksPerExecutor { get; set; } = 10;
    }
}