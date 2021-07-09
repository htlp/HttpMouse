using Microsoft.Extensions.Options;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Client
{
    sealed class HttpMouseClientFactory : IHttpMouseClientFactory
    {
        private const string CLIENT_DOMAIN = "ClientDomain";
        private const string CLIENT_UP_STREAM = "ClientUpstream";
        private const string SERVER_KEY = "ServerKey";
        private readonly IOptionsMonitor<HttpMouseClientOptions> options;

        public HttpMouseClientFactory(IOptionsMonitor<HttpMouseClientOptions> options)
        {
            this.options = options;
        }

        public async Task<IHttpMouseClient> CreateAsync(CancellationToken cancellationToken)
        {
            var opt = this.options.CurrentValue;
            var uriBuilder = new UriBuilder(opt.Server);
            uriBuilder.Scheme = uriBuilder.Scheme == Uri.UriSchemeHttp ? "ws" : "wss";

            var mainConnection = new ClientWebSocket();
            mainConnection.Options.RemoteCertificateValidationCallback = delegate { return true; };
            mainConnection.Options.SetRequestHeader(SERVER_KEY, opt.ServerKey);
            mainConnection.Options.SetRequestHeader(CLIENT_DOMAIN, opt.ClientDomain);
            mainConnection.Options.SetRequestHeader(CLIENT_UP_STREAM, opt.ClientUpstream.ToString());

            await mainConnection.ConnectAsync(uriBuilder.Uri, cancellationToken);
            return new HttpMouseClient(mainConnection, opt);
        }
    }
}
