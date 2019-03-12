using BrandUp.Worker.Executor;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    [Task(ExecutionTimeout = 100)]
    public class TimeoutTask
    {
    }

    public class TimeoutTaskHandler : TaskHandler<TimeoutTask>
    {
        protected override Task OnWorkAsync(TimeoutTask command, CancellationToken cancellationToken)
        {
            return Task.Delay(1000, cancellationToken);
        }
    }
}