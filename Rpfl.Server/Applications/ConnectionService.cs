using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Server.Applications
{
    /// <summary>
    /// 主连接服务
    /// </summary> 
    sealed class ConnectionService
    {
        private const string SERVER_KEY = "ServerKey";
        private const string CLIENT_DOMAIN = "ClientDomain";
        private const string CLIENT_UP_STREAM = "ClientUpstream";

        private readonly IOptionsMonitor<ListenOptions> options;
        private readonly ILogger<ConnectionService> logger;

        private record Connection(Uri Upstream, WebSocket WebSocket);
        private readonly ConcurrentDictionary<string, Connection> connections = new();

        /// <summary>
        /// 主连接服务
        /// </summary>
        /// <param name="logger"></param>
        public ConnectionService(
            IOptionsMonitor<ListenOptions> options,
            ILogger<ConnectionService> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// 收到连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task OnConnectedAsync(HttpContext context, Func<Task> next)
        {
            if (context.WebSockets.IsWebSocketRequest == false ||
                context.Request.Headers.TryGetValue(SERVER_KEY, out var keyValues) == false ||
                context.Request.Headers.TryGetValue(CLIENT_DOMAIN, out var domainValues) == false ||
                context.Request.Headers.TryGetValue(CLIENT_UP_STREAM, out var upSteramValues) == false ||
                Uri.TryCreate(upSteramValues.ToString(), UriKind.Absolute, out var clientUpstream) == false)
            {
                await next();
                return;
            }

            var key = this.options.CurrentValue.Key;
            var cancellationToken = CancellationToken.None;
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            if (string.IsNullOrEmpty(key) == false && key != keyValues.ToString())
            {
                var description = $"Key不正确";
                await this.CloseWebSocketAsync(webSocket, description, cancellationToken);
                return;
            }

            var clientDomain = domainValues.ToString();
            if (this.connections.TryRemove(clientDomain, out var oldConnection))
            {
                var description = $"{clientDomain}在新地方使用";
                await this.CloseWebSocketAsync(oldConnection.WebSocket, description, cancellationToken);
                return;
            }

            this.connections.TryAdd(clientDomain, new Connection(clientUpstream, webSocket));
            this.logger.LogInformation($"{clientDomain}连接过来");
            await this.WaitForCloseAsync(clientDomain, webSocket, cancellationToken);
        }

        /// <summary>
        /// 等待连接关闭
        /// </summary>
        /// <param name="clientDomain"></param>
        /// <param name="webSocket"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WaitForCloseAsync(string clientDomain, WebSocket webSocket, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[4];
                while (cancellationToken.IsCancellationRequested == false)
                {
                    await webSocket.ReceiveAsync(buffer, cancellationToken);
                }
            }
            catch (Exception) when (webSocket.State != WebSocketState.Open)
            {
                if (this.connections.TryRemove(clientDomain, out var connection))
                {
                    connection.WebSocket.Dispose();
                }
            }
        }

        /// <summary>
        /// 关闭websocket
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="description"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CloseWebSocketAsync(WebSocket webSocket, string description, CancellationToken cancellationToken)
        {
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.Empty, description, cancellationToken);
                }
            }
            finally
            {
                webSocket.Dispose();
            }
        }

        /// <summary>
        /// 获取客户端上游地址
        /// </summary>
        /// <param name="clientDomain"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetClientUpStream(string clientDomain, [MaybeNullWhen(false)] out Uri value)
        {
            if (this.connections.TryGetValue(clientDomain, out var connection))
            {
                value = connection.Upstream;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 发送创建传输通道命令
        /// </summary>
        /// <param name="clientDomain"></param>
        /// <param name="channelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SendCreateTransportChannelAsync(string clientDomain, uint channelId, CancellationToken cancellationToken)
        {
            if (this.connections.TryGetValue(clientDomain, out var connection) == false)
            {
                throw new Exception($"远程端{clientDomain}未连接");
            }

            var channelIdBuffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(channelIdBuffer, channelId);
            await connection.WebSocket.SendAsync(channelIdBuffer, WebSocketMessageType.Binary, true, cancellationToken);
        }
    }
}
