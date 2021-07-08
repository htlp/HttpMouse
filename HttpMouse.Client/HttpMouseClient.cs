using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Client
{
    /// <summary>
    /// 客户端
    /// </summary>
    sealed class HttpMouseClient : IHttpMouseClient
    {
        private const string CLIENT_DOMAIN = "ClientDomain";
        private const string CLIENT_UP_STREAM = "ClientUpstream";
        private const string SERVER_KEY = "ServerKey";

        private readonly ILogger<HttpMouseClient> logger;
        private readonly IOptions<HttpMouseClientOptions> options;
        private readonly byte[] connectionIdBuffer = new byte[sizeof(uint)];

        public HttpMouseClient(
            ILogger<HttpMouseClient> logger,
            IOptions<HttpMouseClientOptions> options)
        {
            this.logger = logger;
            this.options = options;
        }

        /// <summary>
        /// 传输数据
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task TransportAsync(CancellationToken stoppingToken)
        {
            using var transportTokenSource = new CancellationTokenSource();
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, transportTokenSource.Token);

            try
            {
                var cancellationToken = linkedTokenSource.Token;
                using var mainConnection = await this.CreateMainConnectionAsync(cancellationToken);

                while (true)
                {
                    var connectionId = await this.ReadConnectionIdAsync(mainConnection, cancellationToken);
                    this.BindingConnectionsAsync(connectionId, cancellationToken);
                }
            }
            catch (Exception)
            {
                transportTokenSource.Cancel();
                throw;
            }
        }

        /// <summary>
        /// 创建服务器连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<ClientWebSocket> CreateMainConnectionAsync(CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder(this.options.Value.Server);
            uriBuilder.Scheme = uriBuilder.Scheme == Uri.UriSchemeHttp ? "ws" : "wss";

            var webSocket = new ClientWebSocket();
            webSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };
            webSocket.Options.SetRequestHeader(SERVER_KEY, this.options.Value.ServerKey);
            webSocket.Options.SetRequestHeader(CLIENT_DOMAIN, this.options.Value.ClientDomain);
            webSocket.Options.SetRequestHeader(CLIENT_UP_STREAM, this.options.Value.ClientUpstream.ToString());

            this.logger.LogInformation($"正在连接到{this.options.Value.Server}");
            await webSocket.ConnectAsync(uriBuilder.Uri, cancellationToken);
            this.logger.LogInformation($"连接到{this.options.Value.Server}成功");
            return webSocket;
        }

        /// <summary>
        /// 读取要创建的反向连接的id
        /// </summary>
        /// <param name="mainConnection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<uint> ReadConnectionIdAsync(ClientWebSocket mainConnection, CancellationToken cancellationToken)
        {
            var result = await mainConnection.ReceiveAsync(this.connectionIdBuffer, cancellationToken);
            return result.MessageType == WebSocketMessageType.Close
                ? throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, result.CloseStatusDescription)
                : BinaryPrimitives.ReadUInt32BigEndian(this.connectionIdBuffer);
        }


        /// <summary>
        /// 绑定上下游的连接进行双向传输
        /// </summary> 
        /// <param name="reverseConnectionId"></param>
        /// <param name="cancellationToken"></param>
        private async void BindingConnectionsAsync(uint connectionId, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Yield();

                using var upConnection = await this.CreateUpConnectionAsync(cancellationToken);
                using var downConnection = await this.CreateDownConnectionAsync(connectionId, cancellationToken);

                var taskX = upConnection.CopyToAsync(downConnection, cancellationToken);
                var taskY = downConnection.CopyToAsync(upConnection, cancellationToken);

                this.logger.LogInformation($"传输通道{connectionId}传输中");
                await Task.WhenAny(taskX, taskY);
                this.logger.LogInformation($"传输通道{connectionId}传输结束");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"传输通道{connectionId}传输结束：{ex.Message}");
            }
        }

        /// <summary>
        /// 创建下游连接
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Stream> CreateDownConnectionAsync(uint connectionId, CancellationToken cancellationToken)
        {
            var server = this.options.Value.Server;
            var endpoint = new DnsEndPoint(server.Host, server.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);

            Stream connection = new NetworkStream(socket, ownsSocket: true);
            if (server.Scheme == Uri.UriSchemeHttps)
            {
                var sslConnection = new SslStream(connection, false, delegate { return true; });
                await sslConnection.AuthenticateAsClientAsync(server.Host);
                connection = sslConnection;
            }

            var connectionIdMemory = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(connectionIdMemory, connectionId);
            await connection.WriteAsync(connectionIdMemory, cancellationToken);

            return connection;
        }

        /// <summary>
        /// 创建上游连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Stream> CreateUpConnectionAsync(CancellationToken cancellationToken)
        {
            var client = this.options.Value.ClientUpstream;
            var endpoint = new DnsEndPoint(client.Host, client.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);

            return new NetworkStream(socket, ownsSocket: true);
        }
    }
}
