using BrandUp.Worker;
using System.Threading.Tasks;

namespace ContosoWorker.Tasks
{
    [Task(StartTimeout = 100)]
    public class TestTask
    {
        // Task properties
        public string Title { get; set; }
    }
}