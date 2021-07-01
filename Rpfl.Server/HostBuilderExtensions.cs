using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Rpfl.Server.Applications;
using Serilog;
using System.IO;

namespace Rpfl.Server
{
    static class HostBuilderExtensions
    {
        public static IHostBuilder UseFileConsoleSerilog(this IHostBuilder hostBuilder, string path = "logs")
        {
            return hostBuilder.UseSerilog((hosting, logger) =>
            {
                var template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                logger.ReadFrom.Configuration(hosting.Configuration)
                  .Enrich.FromLogContext()
                  .WriteTo.Console(outputTemplate: template)
                  .WriteTo.File(Path.Combine(path, @"log.txt"), rollingInterval: RollingInterval.Day, outputTemplate: template);
            });
        }

        public static IWebHostBuilder UseKestrelTransportChannel(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.UseKestrel(kestrel =>
            {
                var transportService = kestrel.ApplicationServices.GetRequiredService<TransportChannelService>();
                var options = kestrel.ApplicationServices.GetRequiredService<IOptions<ListenOptions>>().Value;

                var http = options.Http;
                if (http != null)
                {
                    kestrel.Listen(http.IPAddress, http.Port, listen =>
                    {
                        listen.Use(transportService.OnConnectedAsync);
                    });
                }

                var https = options.Https;
                if (https != null)
                {
                    kestrel.Listen(https.IPAddress, https.Port, listen =>
                    {
                        listen.UseHttps(https.Certificate.Path, https.Certificate.Password);
                        listen.Use(transportService.OnConnectedAsync);
                    });
                }
            });
        }
    }
}
