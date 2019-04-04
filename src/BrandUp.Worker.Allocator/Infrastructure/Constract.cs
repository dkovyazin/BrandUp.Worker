using System;
using System.Collections.Generic;

namespace BrandUp.Worker.Allocator
{
    public class PushCommandResult
    {
        public bool Success { get; private set; }
        public Guid CommandId { get; private set; }
        public bool IsStarted { get; private set; }
        public string Error { get; private set; }

        protected PushCommandResult() { }
        private PushCommandResult(Guid commandId, bool isStarted)
        {
            Success = true;
            CommandId = commandId;
            IsStarted = isStarted;
        }
        private PushCommandResult(string error)
        {
            Success = false;
            Error = error;
        }

        public static PushCommandResult SuccessResult(Guid commandId, bool isStarted)
        {
            return new PushCommandResult(commandId, isStarted);
        }
        public static PushCommandResult ErrorResult(string error)
        {
            return new PushCommandResult(error);
        }
    }
    public class ExecutorOptions
    {
        public string[] CommandTypeNames { get; private set; }

        public ExecutorOptions(string[] commandTypeNames)
        {
            CommandTypeNames = commandTypeNames ?? throw new ArgumentNullException(nameof(commandTypeNames));
        }
    }
    public class WaitTasksResult
    {
        public bool Success { get; private set; }
        public List<CommandToExecute> Tasks { get; private set; }
        public string Error { get; private set; }
        public bool IsEmpty => Success && Tasks.Count == 0;

        protected WaitTasksResult() { }
        private WaitTasksResult(List<CommandToExecute> commands)
        {
            Tasks = commands ?? throw new ArgumentNullException(nameof(commands));
            Success = true;
        }
        private WaitTasksResult(string error)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Success = false;
        }

        public static WaitTasksResult SuccessResult(List<CommandToExecute> commands)
        {
            return new WaitTasksResult(commands);
        }
        public static WaitTasksResult SuccessEmpty()
        {
            return new WaitTasksResult(new List<CommandToExecute>());
        }
        public static WaitTasksResult ErrorResult(string error)
        {
            return new WaitTasksResult(error);
        }
    }
    public class CommandToExecute
    {
        public Guid CommandId { get; private set; }
        public object Command { get; private set; }

        public CommandToExecute(Guid commandId, object command)
        {
            CommandId = commandId;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }
    }
    public class DoneCommandResult
    {
        public bool Success { get; private set; }
        public Guid CommandId { get; private set; }
        public string Error { get; private set; }

        protected DoneCommandResult() { }
        private DoneCommandResult(Guid commandId)
        {
            Success = true;
            CommandId = commandId;
        }
        private DoneCommandResult(string error)
        {
            Success = false;
            Error = error;
        }

        public static DoneCommandResult SuccessResult(Guid commandId)
        {
            return new DoneCommandResult(commandId);
        }
        public static DoneCommandResult ErrorResult(string error)
        {
            return new DoneCommandResult(error);
        }
    }
}