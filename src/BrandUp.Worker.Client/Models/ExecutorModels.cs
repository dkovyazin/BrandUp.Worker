using System;

namespace BrandUp.Worker.Models
{
    public class SubscribeExecutorRequest
    {
        public string[] TaskTypeNames { get; set; }
    }

    public class SubscribeExecutorResponse
    {
        public Guid ExecutorId { get; set; }
    }

    public class WaitTasksRequest
    {
        public Guid ExecutorId { get; set; }
    }

    public class WaitTasksResponse
    {
        public Allocator.TaskExecutionModel[] Tasks { get; set; }
    }

    public class SuccessTaskRequest
    {
        public Guid ExecutorId { get; set; }
        public Guid TaskId { get; set; }
        public TimeSpan ExecutingTime { get; set; }
    }

    public class ErrorTaskRequest
    {
        public Guid ExecutorId { get; set; }
        public Guid TaskId { get; set; }
        public TimeSpan ExecutingTime { get; set; }
    }
}