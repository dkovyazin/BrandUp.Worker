using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.Worker
{
    public interface ITaskHandler
    {
        Task WorkAsync(object command, CancellationToken cancellationToken);
    }
}