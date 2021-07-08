using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse.Applications
{
    /// <summary>
    /// http反向代理服务
    /// </summary>
    sealed class HttpForwarderService
    {
        private readonly IHttpForwarder httpForwarder;
        private readonly ConnectionService connectionService;
        private readonly IOptionsMonitor<HttpMouseOptions> options;
        private readonly HttpMessageInvoker httpClient;
        private readonly ForwarderRequestConfig defaultRequestConfig = new();
        private readonly OptionsTransformer transformer = new();

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
            IOptionsMonitor<HttpMouseOptions> options)
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
                if (this.options.CurrentValue.HttpRequest.TryGetValue(clientDomain, out var requestConfig) == false)
                {
                    requestConfig = this.defaultRequestConfig;
                }
                error = await this.httpForwarder.SendAsync(httpContext, destPrefix, httpClient, requestConfig, this.transformer);
            }

            if (error != ForwarderError.None)
            {
                var serverError = this.options.CurrentValue.Error;
                httpContext.Response.StatusCode = serverError.StatusCode;
                if (File.Exists(serverError.ContentFile) == true)
                {
                    httpContext.Response.ContentType = serverError.ContentType;
                    await httpContext.Response.SendFileAsync(serverError.ContentFile);
                }
            }
        }

        private class OptionsTransformer : HttpTransformer
        {
            private static readonly HttpRequestOptionsKey<string> clientDomainKey = new("ClientDomain");

            public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
            {
                await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);
                proxyRequest.Headers.Host = null;
                proxyRequest.Options.Set(clientDomainKey, httpContext.Request.Host.Host);
            }
        }
    }
}
