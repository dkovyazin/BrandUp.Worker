using System;
using System.Threading.Tasks;

namespace BrandUp.Worker.Tasks
{
    public interface ITaskService
    {
        Task<Guid> PushTask(object taskModel);
    }
}