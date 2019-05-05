using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker
{
    public abstract class TaskHandler<TCommand> : ITaskHandler
        where TCommand : class, new()
    {
        Task ITaskHandler.WorkAsync(object command, CancellationToken cancellationToken)
        {
            return OnWorkAsync((TCommand)command, cancellationToken);
        }

        protected abstract Task OnWorkAsync(TCommand command, CancellationToken cancellationToken);
    }
}