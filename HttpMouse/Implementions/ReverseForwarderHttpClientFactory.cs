using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 反向连接的HttpClient工厂
    /// </summary>
    sealed class ReverseForwarderHttpClientFactory : IForwarderHttpClientFactory, IDisposable
    {
        private readonly IReverseConnectionHandler reverseConnectionHandler;
        private readonly ILogger<ReverseForwarderHttpClientFactory> logger;

        private readonly HttpRequestOptionsKey<string> clientDomainKey = new("ClientDomain");
        private readonly SocketsHttpHandler httpHandler;

        /// <summary>
        /// 反向连接的HttpClient工厂
        /// </summary>
        /// <param name="reverseConnectionHandler"></param>
        /// <param name="logger"></param>
        public ReverseForwarderHttpClientFactory(
            IReverseConnectionHandler reverseConnectionHandler,
            ILogger<ReverseForwarderHttpClientFactory> logger)
        {
            this.reverseConnectionHandler = reverseConnectionHandler;
            this.logger = logger;

            this.httpHandler = new SocketsHttpHandler
            {
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                ConnectCallback = this.ConnectCallback,
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = delegate { return true; }
                }
            };
        }

        /// <summary>
        /// 连接回调
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellation)
        {
            if (context.InitialRequestMessage.Options.TryGetValue(clientDomainKey, out var clientDomain) == false)
            {
                throw new InvalidOperationException($"未设置{nameof(HttpRequestMessage)}的Options：{clientDomainKey.Key}");
            }

            try
            {
                return await this.reverseConnectionHandler.CreateAsync(clientDomain, cancellation);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 创建HttpMessageInvoker
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
        {
            return new HttpMessageInvoker(this.httpHandler, disposeHandler: false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.httpHandler.Dispose();
        }
    }
}
