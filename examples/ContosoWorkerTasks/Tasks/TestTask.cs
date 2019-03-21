using BrandUp.Worker;
using System.Threading.Tasks;

namespace ContosoWorker.Tasks
{
    [Task(TimeoutWaitingToStartInMiliseconds = 100)]
    public class TestTask
    {
    }
}