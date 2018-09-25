using System;
using System.Collections.Generic;

namespace BrandUp.Worker.Executor
{
    public interface ITaskHandlerManager
    {
        IEnumerable<Type> TaskTypes { get; }
    }
}