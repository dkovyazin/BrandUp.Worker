using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker
{
    public interface ITaskService
    {
        Task<Guid> PushTaskAsync(object taskModel, CancellationToken cancellationToken = default);
    }
}