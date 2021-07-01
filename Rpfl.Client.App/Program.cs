using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rpfl.Client.App
{
    class Program
    {
        /// <summary>
        /// 程序入口
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// 创建host
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    services
                        .AddRpfl()
                        .AddHostedService<RpflClientHostedService>()
                        .AddOptions<RpflClientOptions>().Bind(ctx.Configuration.GetSection("Rpfl"));
                });
        }
    }
}
