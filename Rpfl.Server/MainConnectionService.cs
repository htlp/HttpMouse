using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Server
{
    [Service(ServiceLifetime.Singleton)]
    public class MainConnectionService
    {
        private readonly ILogger<MainConnectionService> logger;
        private record MainConnection(Uri Upstream, WebSocket WebSocket);
        private readonly ConcurrentDictionary<string, MainConnection> connections = new();

        public MainConnectionService(ILogger<MainConnectionService> logger)
        {
            this.logger = logger;
        }

        public async Task OnConnectedAsync(HttpContext context)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            if (context.Request.Headers.TryGetValue("ClientUpstream", out var upSteramValues) == false ||
                context.Request.Headers.TryGetValue("ClientDomain", out var domainValues) == false ||
                Uri.TryCreate(upSteramValues.ToString(), UriKind.Absolute, out var clientUpstream) == false)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, "无效的客户端标识", CancellationToken.None);
                return;
            }

            var domain = domainValues.ToString();
            if (this.connections.TryAdd(domain, new MainConnection(clientUpstream, webSocket)) == false)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, "重复客户端的连接实例", CancellationToken.None);
                return;
            }

            try
            {
                var buffer = new byte[1];
                await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            }
            catch (Exception) when (webSocket.State != WebSocketState.Open)
            {
                this.connections.TryRemove(domain, out _);
            }
        }


        public bool TryGetUpStream(string domain, [MaybeNullWhen(false)] out Uri value)
        {
            if (this.connections.TryGetValue(domain, out var connection))
            {
                value = connection.Upstream;
                return true;
            }

            value = default;
            return false;
        }

        public async Task NotifyCreateDataConnectionAsync(string domain, uint connectionId, CancellationToken cancellationToken)
        {
            this.logger.LogInformation($"create connectionId:{connectionId}");
            if (this.connections.TryGetValue(domain, out var connection) == false)
            {
                throw new Exception($"远程端{domain}未连接");
            }

            var idBytes = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(idBytes, connectionId);

            try
            {
                await connection.WebSocket.SendAsync(idBytes, WebSocketMessageType.Binary, true, cancellationToken);
            }
            catch (Exception) when (connection.WebSocket.State != WebSocketState.Open)
            {
                this.connections.TryRemove(domain, out _);
            }
        }
    }
}
