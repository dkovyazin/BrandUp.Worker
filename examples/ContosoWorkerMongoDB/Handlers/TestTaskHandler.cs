using BrandUp.Worker;
using ContosoWorker.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace ContosoWorker.Handlers
{
    public class TestTaskHandler : TaskHandler<TestTask>
    {
        protected override Task OnWorkAsync(TestTask command, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}