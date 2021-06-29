using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Client
{
    sealed class RpflClientHostedService : BackgroundService
    {
        private readonly IConnectionFactory connectionFactory;
        private readonly IOptions<RpflOptions> options;

        public RpflClientHostedService(IConnectionFactory connectionFactory, IOptions<RpflOptions> options)
        {
            this.connectionFactory = connectionFactory;
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
                    await Task.Delay(TimeSpan.FromSeconds(10d), stoppingToken);
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

                var connectionId = buffer.AsMemory(0, result.Count);
                var serverConnection = await this.CreateServerConnectionAsync(connectionId, cancellationToken);
                var clientConnection = await this.CreateClientConnectionAsync(cancellationToken);

                DoTransportAsync(serverConnection, clientConnection, cancellationToken);
            }
        }

        private static async void DoTransportAsync(ConnectionContext server, ConnectionContext client, CancellationToken cancellationToken)
        {
            await BindTransportAsync(server.Transport, client.Transport, cancellationToken);
            await server.DisposeAsync();
            await client.DisposeAsync();
        }

        private static Task BindTransportAsync(IDuplexPipe server, IDuplexPipe client, CancellationToken cancellationToken)
        {
            var task1 = ReadWriteAsync(server.Input, client.Output, cancellationToken);
            var task2 = ReadWriteAsync(client.Input, server.Output, cancellationToken);
            return Task.WhenAny(task1, task2);
        }

        private static async Task ReadWriteAsync(PipeReader reader, PipeWriter writer, CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                var result = await reader.ReadAsync(cancellationToken);
                if (result.IsCanceled || result.IsCompleted)
                {
                    break;
                }

                foreach (var memory in result.Buffer)
                {
                    await writer.WriteAsync(memory, cancellationToken);
                }

                reader.AdvanceTo(result.Buffer.End);
            }
        }

        private async Task<ConnectionContext> CreateServerConnectionAsync(ReadOnlyMemory<byte> connectionId, CancellationToken cancellationToken)
        {
            var server = this.options.Value.Server;
            var addresses = await Dns.GetHostAddressesAsync(server.Host);
            if (addresses.Length == 0)
            {
                throw new Exception("无法解析域名{server.Host}");
            }
            var endpoint = new IPEndPoint(addresses.Last(), server.Port);
            var connection = await this.connectionFactory.ConnectAsync(endpoint, cancellationToken);
            await connection.Transport.Output.WriteAsync(connectionId, cancellationToken);
            return connection;
        }

        private async Task<ConnectionContext> CreateClientConnectionAsync(CancellationToken cancellationToken)
        {
            var client = this.options.Value.ClientUpstream;
            var addresses = await Dns.GetHostAddressesAsync(client.Host);
            if (addresses.Length == 0)
            {
                throw new Exception("无法解析域名{server.Host}");
            }
            var endpoint = new IPEndPoint(addresses.Last(), client.Port);
            return await this.connectionFactory.ConnectAsync(endpoint, cancellationToken);
        }
    }
}
