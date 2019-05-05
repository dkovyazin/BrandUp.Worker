using System.Threading.Tasks;
using Xunit;

namespace BrandUp.Worker.Testing
{
    public class HandlerTestTests
    {
        [Fact]
        public async Task ExecuteTask_Success()
        {
            using (var e = new HandlerTest<SuccessTask, SuccessTaskHandler>())
            {
                var result = await e.ExecuteTaskAsync(new SuccessTask());

                Assert.Equal(TaskExecutionStatus.Success, result);
            }
        }

        [Fact]
        public async Task ExecuteTask_Error()
        {
            using (var e = new HandlerTest<ErrorTask, ErrorTaskHandler>())
            {
                var result = await e.ExecuteTaskAsync(new ErrorTask());

                Assert.Equal(TaskExecutionStatus.Error, result);
            }
        }

        [Fact]
        public async Task ExecuteTask_Timeout()
        {
            using (var e = new HandlerTest<TimeoutTask, TimeoutTaskHandler>())
            {
                var result = await e.ExecuteTaskAsync(new TimeoutTask());

                Assert.Equal(TaskExecutionStatus.Timeout, result);
            }
        }
    }
}