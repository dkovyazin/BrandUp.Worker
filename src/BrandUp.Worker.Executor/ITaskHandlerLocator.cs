using System;
using System.Collections.Generic;

namespace BrandUp.Worker.Executor
{
    public interface ITaskHandlerLocator
    {
        IEnumerable<Type> TaskTypes { get; }
    }
}