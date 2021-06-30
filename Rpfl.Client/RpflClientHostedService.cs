using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Client
{
    sealed class RpflClientHostedService : BackgroundService
    {
        private readonly ILogger<RpflClientHostedService> logger;
        private readonly IOptions<RpflOptions> options;

        public RpflClientHostedService(
            ILogger<RpflClientHostedService> logger,
            IOptions<RpflOptions> options)
        {
            this.logger = logger;
            this.options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    await this.TransportAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex.Message);
                    await Task.Delay(this.options.Value.ReconnectDueTime, stoppingToken);
                }
            }
        }

        private async Task TransportAsync(CancellationToken cancellationToken)
        {
            var builder = new UriBuilder(this.options.Value.Server);
            builder.Scheme = builder.Scheme == "http" ? "ws" : "wss";

            using var webSocket = new ClientWebSocket();
            webSocket.Options.SetRequestHeader("ClientDomain", this.options.Value.ClientDomain);
            webSocket.Options.SetRequestHeader("ClientUpStream", this.options.Value.ClientUpstream.ToString());

            await webSocket.ConnectAsync(builder.Uri, cancellationToken);
            var buffer = new byte[8 * 1024];
            while (cancellationToken.IsCancellationRequested == false)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                var channelId = buffer.AsMemory(0, result.Count);
                var server = await this.CreateServerChannelAsync(channelId, cancellationToken);
                var client = await this.CreateClientChannelAsync(cancellationToken);

                this.BindTransportAsync(server, client, cancellationToken);
            }
        }

        private async void BindTransportAsync(Stream server, Stream client, CancellationToken cancellationToken)
        {
            try
            {
                this.logger.LogInformation("传输进行中");
                var task1 = server.CopyToAsync(client, cancellationToken);
                var task2 = client.CopyToAsync(server, cancellationToken);
                await Task.WhenAny(task1, task2);
                this.logger.LogWarning("传输结束");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"传输结束：{ex.Message}");
                await server.DisposeAsync();
                await client.DisposeAsync();
            }
        }


        private async Task<Stream> CreateServerChannelAsync(ReadOnlyMemory<byte> channelId, CancellationToken cancellationToken)
        {
            var server = this.options.Value.Server;
            var addresses = await Dns.GetHostAddressesAsync(server.Host);
            if (addresses.Length == 0)
            {
                throw new Exception("无法解析域名{server.Host}");
            }
            var endpoint = new IPEndPoint(addresses.Last(), server.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);
            await socket.SendAsync(channelId, SocketFlags.None, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }

        private async Task<Stream> CreateClientChannelAsync(CancellationToken cancellationToken)
        {
            var client = this.options.Value.ClientUpstream;


            var addresses = await Dns.GetHostAddressesAsync(client.Host);
            if (addresses.Length == 0)
            {
                throw new Exception("无法解析域名{server.Host}");
            }
            var endpoint = new IPEndPoint(addresses.Last(), client.Port);

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
    }
}
