using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Testing
{
    [Task]
    public class SuccessTask
    {
    }

    public class SuccessTaskHandler : TaskHandler<SuccessTask>
    {
        protected override Task OnWorkAsync(SuccessTask command, CancellationToken cancellationToken)
        {
            return Task.Delay(500, cancellationToken);
        }
    }
}