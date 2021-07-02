using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace Rpfl.Server.Applications
{
    /// <summary>
    /// http反向代理服务
    /// </summary>
    sealed class HttpForwarderService
    {
        private readonly IHttpForwarder httpForwarder;
        private readonly ConnectionService connectionService;
        private readonly HttpMessageInvoker httpClient;
        private readonly ForwarderRequestConfig forwarderRequestConfig = new();

        /// <summary>
        /// http反向代理服务
        /// </summary>
        /// <param name="httpForwarder"></param>
        /// <param name="connectionService"></param>
        /// <param name="transportChannelService"></param>
        public HttpForwarderService(
            IHttpForwarder httpForwarder,
            ConnectionService connectionService,
            TransportChannelService transportChannelService)
        {
            this.httpForwarder = httpForwarder;
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
        /// 发送http数据
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task SendAsync(HttpContext httpContext, Func<Task> _)
        {
            var clientDomain = httpContext.Request.Host.Host;
            if (this.connectionService.TryGetClientUpStream(clientDomain, out var clientUpstream) == false)
            {
                var problem = new
                {
                    type = "http://www.restapitutorial.com/httpstatuscodes.html",
                    title = "服务不可用",
                    detail = "上游代理服务未连接",
                    status = StatusCodes.Status503ServiceUnavailable,
                    instance = $"{httpContext.Request.Path}{httpContext.Request.QueryString}"
                };
                httpContext.Response.ContentType = "application/problem+json";
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await httpContext.Response.WriteAsJsonAsync(problem);
            }
            else
            {
                var destPrefix = clientUpstream.ToString();
                var transformer = new HostTransformer(clientUpstream.Host, clientDomain);
                await this.httpForwarder.SendAsync(httpContext, destPrefix, httpClient, this.forwarderRequestConfig, transformer);
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
