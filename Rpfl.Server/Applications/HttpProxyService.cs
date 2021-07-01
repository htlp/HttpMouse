using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Service.Proxy;

namespace Rpfl.Server.Applications
{
    /// <summary>
    /// http反向代理服务
    /// </summary>
    sealed class HttpProxyService
    {
        private readonly IHttpProxy httpProxy;
        private readonly ConnectionService connectionService;
        private readonly HttpMessageInvoker httpClient;
        private readonly RequestProxyOptions requestProxyOptions = new();

        /// <summary>
        /// http反向代理服务
        /// </summary>
        /// <param name="httpProxy"></param>
        /// <param name="connectionService"></param>
        /// <param name="transportChannelService"></param>
        public HttpProxyService(
            IHttpProxy httpProxy,
            ConnectionService connectionService,
            TransportChannelService transportChannelService)
        {
            this.httpProxy = httpProxy;
            this.connectionService = connectionService;
            this.httpClient = CreateHttpClient(transportChannelService);
        }

        private static HttpMessageInvoker CreateHttpClient(TransportChannelService transportChannelService)
        {
            return new HttpMessageInvoker(new SocketsHttpHandler()
            {
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                ConnectCallback = transportChannelService.CreateChannelAsync,
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = delegate { return true; }
                }
            });
        }

        /// <summary>
        /// http代理
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task ProxyAsync(HttpContext httpContext)
        {
            var clientDomain = httpContext.Request.Host.Host;
            if (this.connectionService.TryGetClientUpStream(clientDomain, out var clientUpstream) == false)
            {
                httpContext.Response.ContentType = "text/plain;charset=UTF-8";
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await httpContext.Response.WriteAsync("上游服务未连接");
            }
            else
            {
                var destPrefix = clientUpstream.ToString();
                var transformer = new HostTransformer(clientUpstream.Host, clientDomain);
                await this.httpProxy.ProxyAsync(httpContext, destPrefix, httpClient, this.requestProxyOptions, transformer);
            }
        }

        private class HostTransformer : HttpTransformer
        {
            private readonly string requestHost;
            private readonly string clientDomain;

            public HostTransformer(string requestHost, string clientDomain)
            {
                this.requestHost = requestHost;
                this.clientDomain = clientDomain;
            }

            public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
            {
                await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);
                proxyRequest.Headers.Host = this.requestHost;
                proxyRequest.Options.TryAdd("ClientDomain", this.clientDomain);
            }
        }
    }
}
