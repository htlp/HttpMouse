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

namespace HttpMouse.Connections
{
    /// <summary>
    /// 主连接服务
    /// </summary> 
    sealed class MainConnectionService
    {
        private const string SERVER_KEY = "ServerKey";
        private const string CLIENT_DOMAIN = "ClientDomain";
        private const string CLIENT_UP_STREAM = "ClientUpstream";

        private readonly IOptionsMonitor<HttpMouseOptions> options;
        private readonly ILogger<MainConnectionService> logger;
        private readonly ConcurrentDictionary<string, MainConnection> connections = new();

        /// <summary>
        /// 主连接服务
        /// </summary>
        /// <param name="logger"></param>
        public MainConnectionService(
            IOptionsMonitor<HttpMouseOptions> options,
            ILogger<MainConnectionService> logger)
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

            var clientDomain = domainValues.ToString();
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var connection = new MainConnection(clientDomain, clientUpstream, webSocket);

            // 密钥验证
            var key = this.options.CurrentValue.Key;
            if (string.IsNullOrEmpty(key) == false && key != keyValues.ToString())
            {
                await connection.CloseAsync("Key不正确");
                return;
            }

            // 验证连接唯一
            if (this.connections.TryAdd(clientDomain, connection) == false)
            {
                await connection.CloseAsync($"已在其它地方存在{clientDomain}的连接实例");
                return;
            }

            this.logger.LogInformation($"{connection}连接过来");
            await connection.WaitingCloseAsync();

            this.logger.LogInformation($"{connection}断开连接");
            this.connections.TryRemove(clientDomain, out _);
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
