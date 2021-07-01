﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Client
{
    /// <summary>
    /// 客户端
    /// </summary>
    sealed class RpflClient : IRpflClient
    {
        private const string CLIENT_DOMAIN = "ClientDomain";
        private const string CLIENT_UP_STREAM = "ClientUpstream";

        private readonly ILogger<RpflClient> logger;
        private readonly IOptions<RpflClientOptions> options;
        private readonly byte[] channelIdBuffer = new byte[sizeof(uint)];

        public RpflClient(
            ILogger<RpflClient> logger,
            IOptions<RpflClientOptions> options)
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
            using var tunnelTokenSource = new CancellationTokenSource();
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, tunnelTokenSource.Token);

            try
            {
                var cancellationToken = linkedTokenSource.Token;
                using var connection = await this.CreateConnectionAsync(cancellationToken);

                while (true)
                {
                    var channelId = await this.ReadChannelIdAsync(connection, cancellationToken);
                    this.TunnelAsync(channelId, cancellationToken);
                }
            }
            catch (Exception)
            {
                tunnelTokenSource.Cancel();
                throw;
            }
        }

        /// <summary>
        /// 创建服务器连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<ClientWebSocket> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            var uriBuilder = new UriBuilder(this.options.Value.Server);
            uriBuilder.Scheme = uriBuilder.Scheme == Uri.UriSchemeHttp ? "ws" : "wss";

            var webSocket = new ClientWebSocket();
            webSocket.Options.RemoteCertificateValidationCallback = delegate { return true; };
            webSocket.Options.SetRequestHeader(CLIENT_DOMAIN, this.options.Value.ClientDomain);
            webSocket.Options.SetRequestHeader(CLIENT_UP_STREAM, this.options.Value.ClientUpstream.ToString());
            await webSocket.ConnectAsync(uriBuilder.Uri, cancellationToken);
            return webSocket;
        }

        /// <summary>
        /// 读取要创建的传输通道id
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<uint> ReadChannelIdAsync(ClientWebSocket connection, CancellationToken cancellationToken)
        {
            var result = await connection.ReceiveAsync(this.channelIdBuffer, cancellationToken);
            return result.MessageType == WebSocketMessageType.Close
                ? throw new WebSocketException(WebSocketError.Faulted, "连接已断开")
                : BinaryPrimitives.ReadUInt32BigEndian(this.channelIdBuffer);
        }


        /// <summary>
        /// 绑定传输通道进行传输
        /// </summary> 
        /// <param name="channelId"></param>
        /// <param name="cancellationToken"></param>
        private async void TunnelAsync(uint channelId, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Yield();

                this.logger.LogInformation($"正在创建传输通道{channelId}");
                using var serverChannel = await this.CreateServerChannelAsync(channelId, cancellationToken);
                using var clientChannel = await this.CreateClientChannelAsync(cancellationToken);

                this.logger.LogInformation($"传输通道{channelId}传输进行中");
                var taskX = serverChannel.CopyToAsync(clientChannel, cancellationToken);
                var taskY = clientChannel.CopyToAsync(serverChannel, cancellationToken);
                await Task.WhenAny(taskX, taskY);

                this.logger.LogInformation($"传输通道{channelId}传输结束");
            }
            catch (Exception)
            {
                this.logger.LogWarning($"传输通道{channelId}传输结束");
            }
        }

        /// <summary>
        /// 创建服务端传输通道
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Stream> CreateServerChannelAsync(uint channelId, CancellationToken cancellationToken)
        {
            var server = this.options.Value.Server;
            var endpoint = new DnsEndPoint(server.Host, server.Port);
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint, cancellationToken);

            var channelIdMemory = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(channelIdMemory, channelId);
            await socket.SendAsync(channelIdMemory, SocketFlags.None, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }

        /// <summary>
        /// 创建客户端传输通道
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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