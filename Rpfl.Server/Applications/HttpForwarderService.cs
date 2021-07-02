using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
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
        private readonly IOptionsMonitor<ServerOptions> options;
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
            TransportChannelService transportChannelService,
            IOptionsMonitor<ServerOptions> options)
        {
            this.httpForwarder = httpForwarder;
            this.connectionService = connectionService;
            this.options = options;
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
        public async Task SendAsync(HttpContext httpContext, Func<Task> next)
        {
            var error = ForwarderError.NoAvailableDestinations;
            var clientDomain = httpContext.Request.Host.Host;
            if (this.connectionService.TryGetClientUpStream(clientDomain, out var clientUpstream))
            {
                var destPrefix = clientUpstream.ToString();
                var transformer = new OptionsTransformer(clientDomain);
                error = await this.httpForwarder.SendAsync(httpContext, destPrefix, httpClient, this.forwarderRequestConfig, transformer);
            }

            if (error != ForwarderError.None)
            {
                var serverError = this.options.CurrentValue.Error;
                httpContext.Response.StatusCode = serverError.StatusCode;
                httpContext.Response.ContentType = serverError.ContentType;
                await httpContext.Response.SendFileAsync(serverError.ContentFile);
            }
        }

        private class OptionsTransformer : HttpTransformer
        {
            private readonly string clientDomain;
            private static readonly HttpRequestOptionsKey<string> clientDomainKey = new("ClientDomain");

            public OptionsTransformer(string clientDomain)
            {
                this.clientDomain = clientDomain;
            }

            public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
            {
                await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);
                proxyRequest.Headers.Host = null;
                proxyRequest.Options.Set(clientDomainKey, this.clientDomain);
            }
        }
    }
}
