using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;

namespace HttpMouse
{
    static class HostBuilderExtensions
    {
        public static IWebHostBuilder UseKestrelReverseConnection(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.UseKestrel(kestrel =>
            {
                var options = kestrel.ApplicationServices.GetRequiredService<IOptions<HttpMouseOptions>>().Value;

                var http = options.Listen.Http;
                if (http != null)
                {
                    kestrel.Listen(http.IPAddress, http.Port);
                }

                var https = options.Listen.Https;
                if (https != null && File.Exists(https.Certificate.Path))
                {
                    kestrel.Listen(https.IPAddress, https.Port, listen =>
                    {
                        listen.Protocols = HttpProtocols.Http1AndHttp2;
                        listen.UseHttps(https.Certificate.Path, https.Certificate.Password);
                    });
                }
            });
        }
    }
}
