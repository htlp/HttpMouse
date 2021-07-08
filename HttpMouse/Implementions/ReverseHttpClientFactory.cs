using Microsoft.Extensions.Logging;
using System.Net.Http;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 反向连接的HttpClient工厂
    /// </summary>
    sealed class ReverseHttpClientFactory : ForwarderHttpClientFactory, IForwarderHttpClientFactory
    {
        private readonly IReverseConnectionService reverseConnectionService;

        /// <summary>
        /// 反向连接的HttpClient工厂
        /// </summary>
        /// <param name="reverseConnectionService"></param>
        /// <param name="logger"></param>
        public ReverseHttpClientFactory(
            IReverseConnectionService reverseConnectionService,
            ILogger<ForwarderHttpClientFactory> logger)
            : base(logger)
        {
            this.reverseConnectionService = reverseConnectionService;
        }

        /// <summary>
        /// 配置连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="handler"></param>
        protected override void ConfigureHandler(ForwarderHttpClientContext context, SocketsHttpHandler handler)
        {
            base.ConfigureHandler(context, handler);
            handler.ConnectCallback = this.reverseConnectionService.CreateReverseConnectionAsync;
        }
    }
}
