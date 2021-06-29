using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Service.Proxy;

namespace Rpfl.Server
{
    [Service(ServiceLifetime.Singleton)]
    public class ProxyService
    {
        private readonly HttpMessageInvoker httpClient;
        private readonly IHttpProxy httpProxy;
        private readonly MainConnectionService mainConnectionService;

        public ProxyService(
            IHttpProxy httpProxy,
            MainConnectionService mainConnectionService,
            DataConnectionService dataConnectionService)
        {
            this.httpProxy = httpProxy;
            this.mainConnectionService = mainConnectionService;
            this.httpClient = CreateHttpClient(dataConnectionService);
        }

        private static HttpMessageInvoker CreateHttpClient(DataConnectionService dataConnectionService)
        {
            return new HttpMessageInvoker(new SocketsHttpHandler()
            {
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                ConnectCallback = dataConnectionService.CreateConnectionAsync
            });
        }

        public async Task ProxyAsync(HttpContext httpContext)
        {
            var domain = httpContext.Request.Host.Host;
            if (this.mainConnectionService.TryGetUpStream(domain, out var upstream) == false)
            {
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await httpContext.Response.WriteAsync("上游服务未连接");
            }
            else
            {
                var destPrefix = upstream.ToString();
                var requestProxyOptions = new RequestProxyOptions { Timeout = TimeSpan.FromSeconds(20d) };
                await this.httpProxy.ProxyAsync(httpContext, destPrefix, httpClient, requestProxyOptions);
            }
        }
    }
}
