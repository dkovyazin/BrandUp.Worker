using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker.Executor
{
    internal interface ITaskHandler : IDisposable
    {
        Task WorkAsync(object command, CancellationToken cancellationToken);
    }

    public abstract class TaskHandler<TCommand> : ITaskHandler
        where TCommand : class, new()
    {
        Task ITaskHandler.WorkAsync(object command, CancellationToken cancellationToken)
        {
            return OnWorkAsync((TCommand)command, cancellationToken);
        }

        protected abstract Task OnWorkAsync(TCommand command, CancellationToken cancellationToken);

        #region IDisposable members

        private bool _isDisposed = false;

        void IDisposable.Dispose()
        {
            if (!_isDisposed)
            {
                OnDisposing();

                _isDisposed = true;
            }
        }

        protected virtual void OnDisposing()
        {
        }

        #endregion
    }
}
