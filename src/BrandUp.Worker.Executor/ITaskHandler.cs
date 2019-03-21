using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker
{
    internal interface ITaskHandler : IDisposable
    {
        Task WorkAsync(object command, CancellationToken cancellationToken);
    }
}
