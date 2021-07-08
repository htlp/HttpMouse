using HttpMouse.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse.HttpForwarders
{
    /// <summary>
    /// http反向代理服务
    /// </summary>
    sealed class HttpForwarderService
    {
        private readonly IHttpForwarder httpForwarder;
        private readonly MainConnectionService mainConnectionService;
        private readonly IOptionsMonitor<HttpMouseOptions> options;
        private readonly HttpMessageInvoker httpClient;
        private readonly ForwarderRequestConfig defaultRequestConfig = new();
        private readonly OptionsTransformer transformer = new();

        /// <summary>
        /// http反向代理服务
        /// </summary>
        /// <param name="httpForwarder"></param>
        /// <param name="mainConnectionService"></param>
        /// <param name="httpClientFactory"></param>
        /// <param name="options"></param>
        public HttpForwarderService(
            IHttpForwarder httpForwarder,
            MainConnectionService mainConnectionService,
            HttpClientFactory httpClientFactory,
            IOptionsMonitor<HttpMouseOptions> options)
        {
            this.httpForwarder = httpForwarder;
            this.mainConnectionService = mainConnectionService;
            this.httpClient = httpClientFactory.CreateHttpClient();
            this.options = options;
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
            if (this.mainConnectionService.TryGetClientUpStream(clientDomain, out var clientUpstream))
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
    }
}
