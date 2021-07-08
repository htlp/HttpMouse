using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System.IO;

namespace HttpMouse
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

        public static IWebHostBuilder UseKestrelReverseConnection(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.UseKestrel(kestrel =>
            {
                var reverseConnectionService = kestrel.ApplicationServices.GetRequiredService<IReverseConnectionService>();
                var options = kestrel.ApplicationServices.GetRequiredService<IOptions<HttpMouseOptions>>().Value;

                var http = options.Listen.Http;
                if (http != null)
                {
                    kestrel.Listen(http.IPAddress, http.Port, listen =>
                    {
                        listen.Use(reverseConnectionService.HandleKestrelConnectionAsync);
                    });
                }

                var https = options.Listen.Https;
                if (https != null && File.Exists(https.Certificate.Path))
                {
                    kestrel.Listen(https.IPAddress, https.Port, listen =>
                    {
                        listen.Protocols = HttpProtocols.Http1AndHttp2;
                        listen.UseHttps(https.Certificate.Path, https.Certificate.Password);
                        listen.Use(reverseConnectionService.HandleKestrelConnectionAsync);
                    });
                }
            });
        }
    }
}
