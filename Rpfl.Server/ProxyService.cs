using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        public ProxyService(
            IHttpProxy httpProxy,
            DataConnectionService dataConnectionService)
        {
            this.httpProxy = httpProxy;
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
            var destPrefix = $"http://{httpContext.Request.Host.Host}:5000/";
            var requestProxyOptions = new RequestProxyOptions { Timeout = TimeSpan.FromSeconds(20d) };
            await this.httpProxy.ProxyAsync(httpContext, destPrefix, httpClient, requestProxyOptions);
        }
    }
}
