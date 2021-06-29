using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Rpfl.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel(kestrel =>
                    {
                        kestrel.ConfigurationLoader.Load();
                        var listenOptions = kestrel.GetType()
                            .GetProperty("ListenOptions", BindingFlags.Instance | BindingFlags.NonPublic)?
                            .GetValue(kestrel);

                        if (listenOptions is IEnumerable<ListenOptions> options)
                        {
                            var service = kestrel.ApplicationServices.GetRequiredService<DataConnectionService>();
                            foreach (var listen in options)
                            {
                                listen.Use(service.OnConnectedAsync);
                            }
                        }
                    });
                })
                .UseSerilog((hosting, logger) =>
                {
                    var template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                    logger.ReadFrom.Configuration(hosting.Configuration)
                      .Enrich.FromLogContext()
                      .WriteTo.Console(outputTemplate: template)
                      .WriteTo.File(Path.Combine("logs", @"log.txt"), rollingInterval: RollingInterval.Day, outputTemplate: template);
                });
        }
    }
}
