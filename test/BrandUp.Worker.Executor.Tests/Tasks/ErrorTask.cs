using BrandUp.Worker.Executor;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    [Task]
    public class ErrorTask
    {
    }

    public class ErrorTaskHandler : TaskHandler<ErrorTask>
    {
        protected override Task OnWorkAsync(ErrorTask command, CancellationToken cancellationToken)
        {
            throw new Exception("Error");
        }
    }
}