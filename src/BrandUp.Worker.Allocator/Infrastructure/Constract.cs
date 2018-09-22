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
    public class ConnectExecutorResult
    {
        public bool Success { get; private set; }
        public Guid ExecutorId { get; private set; }
        public string Error { get; private set; }

        protected ConnectExecutorResult() { }
        private ConnectExecutorResult(Guid executorId)
        {
            Success = true;
            ExecutorId = executorId;
        }
        private ConnectExecutorResult(string error)
        {
            Success = false;
            Error = error;
        }

        public static ConnectExecutorResult SuccessResult(Guid executorId)
        {
            return new ConnectExecutorResult(executorId);
        }
        public static ConnectExecutorResult ErrorResult(string error)
        {
            return new ConnectExecutorResult(error);
        }
    }
    public class WaitCommandsResult
    {
        public bool Success { get; private set; }
        public List<CommandToExecute> Commands { get; private set; }
        public string Error { get; private set; }
        public bool IsEmpty => Success && Commands.Count == 0;

        protected WaitCommandsResult() { }
        private WaitCommandsResult(List<CommandToExecute> commands)
        {
            Commands = commands ?? throw new ArgumentNullException(nameof(commands));
            Success = true;
        }
        private WaitCommandsResult(string error)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Success = false;
        }

        public static WaitCommandsResult SuccessResult(List<CommandToExecute> commands)
        {
            return new WaitCommandsResult(commands);
        }
        public static WaitCommandsResult SuccessEmpty()
        {
            return new WaitCommandsResult(new List<CommandToExecute>());
        }
        public static WaitCommandsResult ErrorResult(string error)
        {
            return new WaitCommandsResult(error);
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