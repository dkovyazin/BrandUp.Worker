using System;

namespace BrandUp.Worker.Allocator
{
    public class TaskAllocatorOptions
    {
        public TimeSpan DefaultTaskWaitingTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxTasksPerExecutor { get; set; } = 10;
    }
}