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
        private readonly ClientWebSocket mainConnection;
        private readonly HttpMouseClientOptions options;
        private readonly CancellationTokenSource disposeCancellationTokenSource = new();

        /// <summary>
        /// 获取是否连接
        /// </summary>
        public bool IsConnected => this.mainConnection.State == WebSocketState.Open;

        /// <summary>
        /// 客户端
        /// </summary>
        /// <param name="mainConnection"></param>
        /// <param name="options"></param>
        public HttpMouseClient(ClientWebSocket mainConnection, HttpMouseClientOptions options)
        {
            this.mainConnection = mainConnection;
            this.options = options;
        }

        /// <summary>
        /// 传输数据
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task TransportAsync(CancellationToken cancellationToken)
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.disposeCancellationTokenSource.Token);
            try
            {
                while (true)
                {
                    var connectionId = await this.ReadConnectionIdAsync(cancellationTokenSource.Token);
                    this.TransportAsync(connectionId, cancellationTokenSource.Token);
                }
            }
            catch (Exception)
            {
                cancellationTokenSource.Cancel();
                throw;
            }
        }

        /// <summary>
        /// 读取要创建的反向连接的id
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<uint> ReadConnectionIdAsync(CancellationToken cancellationToken)
        {
            var connectionIdBuffer = new byte[sizeof(uint)];
            var result = await this.mainConnection.ReceiveAsync(connectionIdBuffer, cancellationToken);

            return result.MessageType == WebSocketMessageType.Close
                ? throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, result.CloseStatusDescription)
                : BinaryPrimitives.ReadUInt32BigEndian(connectionIdBuffer);
        }


        /// <summary>
        /// 绑定上下游的连接进行双向传输
        /// </summary> 
        /// <param name="reverseConnectionId"></param>
        /// <param name="cancellationToken"></param>
        private async void TransportAsync(uint connectionId, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Yield();

                using var upConnection = await this.CreateUpConnectionAsync(cancellationToken);
                using var downConnection = await this.CreateDownConnectionAsync(connectionId, cancellationToken);

                var taskX = upConnection.CopyToAsync(downConnection, cancellationToken);
                var taskY = downConnection.CopyToAsync(upConnection, cancellationToken);

                await Task.WhenAny(taskX, taskY);
            }
            catch (Exception)
            {
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
            var server = this.options.Server;
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
            var client = this.options.ClientUpstream;
            var endpoint = new DnsEndPoint(client.Host, client.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);

            return new NetworkStream(socket, ownsSocket: true);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.disposeCancellationTokenSource.Cancel();
            this.disposeCancellationTokenSource.Dispose();
            this.mainConnection.Dispose();
        }
    }
}
