using System;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocatorOptions
    {
        public TimeSpan TimeoutWaitingTasksPerExecutor { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxTasksPerExecutor { get; set; } = 10;
    }
}