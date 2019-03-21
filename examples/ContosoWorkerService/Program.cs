using Microsoft.AspNetCore.Hosting;

namespace ContosoWorker.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return Microsoft.AspNetCore.WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
        }
    }
}