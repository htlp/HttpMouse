using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HttpMouse.ServerHost
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((hosting, logger) =>
                {
                    const string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                    logger.ReadFrom.Configuration(hosting.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(outputTemplate: template)
                        .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day, outputTemplate: template);
                });
        }
    }
}
