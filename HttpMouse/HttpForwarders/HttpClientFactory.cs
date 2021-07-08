using HttpMouse.Connections;
using System.Net;
using System.Net.Http;
using System.Net.Security;

namespace HttpMouse.HttpForwarders
{
    sealed class HttpClientFactory
    {
        private readonly ReverseConnectionService reverseConnectionService;

        public HttpClientFactory(ReverseConnectionService reverseConnectionService)
        {
            this.reverseConnectionService = reverseConnectionService;
        } 

        public HttpMessageInvoker CreateHttpClient()
        {
            var httpHandler = new SocketsHttpHandler()
            {
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                ConnectCallback = this.reverseConnectionService.CreateReverseConnectionAsync,
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = delegate { return true; }
                }
            };
            return new HttpMessageInvoker(httpHandler);
        }
    }
}
