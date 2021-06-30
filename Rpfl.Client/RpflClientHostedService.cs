using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
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
        private readonly byte[] channelIdBuffer = new byte[1024];

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
                using var cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cancellationTokenSource.Token);
                    await this.TransportAsync(linkedTokenSource.Token);
                }
                catch (Exception ex)
                {
                    cancellationTokenSource.Cancel();
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

            using var cancellationTokenSource = new CancellationTokenSource();
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);


            while (cancellationToken.IsCancellationRequested == false)
            {
                var result = await webSocket.ReceiveAsync(this.channelIdBuffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                var channelId = this.channelIdBuffer.AsMemory(0, result.Count);
                var server = await this.CreateServerChannelAsync(channelId, cancellationToken);
                var client = await this.CreateClientChannelAsync(cancellationToken);

                this.BindChannelsAsync(server, client, linkedTokenSource.Token);
            }
        }

        /// <summary>
        /// 绑定传输通道
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <param name="cancellationToken"></param>
        private async void BindChannelsAsync(Stream server, Stream client, CancellationToken cancellationToken)
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
            var endpoint = new DnsEndPoint(server.Host, server.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);

            await socket.SendAsync(channelId, SocketFlags.None, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }

        private async Task<Stream> CreateClientChannelAsync(CancellationToken cancellationToken)
        {
            var client = this.options.Value.ClientUpstream;
            var endpoint = new DnsEndPoint(client.Host, client.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);

            return new NetworkStream(socket, ownsSocket: true);
        }
    }
}
