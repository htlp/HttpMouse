using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 基于websocket的主连接
    /// </summary>
    sealed class MainConnection : IMainConnection
    {
        private readonly WebSocket webSocket;
        private readonly IOptionsMonitor<HttpMouseOptions> options;

        /// <summary>
        /// 获取绑定的域名
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// 获取上游地址
        /// </summary>
        public Uri Upstream { get; }

        /// <summary>
        /// 基于websocket的主连接
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="upstream"></param>
        /// <param name="webSocket"></param>
        /// <param name="options"></param>
        public MainConnection(
            string domain,
            Uri upstream,
            WebSocket webSocket,
            IOptionsMonitor<HttpMouseOptions> options)
        {
            this.Domain = domain;
            this.Upstream = upstream;
            this.webSocket = webSocket;
            this.options = options;
        }

        /// <summary>
        /// 转换为ClusterConfig
        /// </summary>
        /// <returns></returns>
        public ClusterConfig ToClusterConfig()
        {
            var address = this.Upstream.ToString();
            var destinations = new Dictionary<string, DestinationConfig>
            {
                [this.Domain] = new DestinationConfig { Address = address }
            };

            if (this.options.CurrentValue.HttpRequest.TryGetValue(this.Domain, out var httpRequest) == false)
            {
                httpRequest = ForwarderRequestConfig.Empty;
            }

            return new ClusterConfig
            {
                ClusterId = this.Domain,
                Destinations = destinations,
                HttpRequest = httpRequest
            };
        }

        /// <summary>
        /// 转换为RouteConfig
        /// </summary>
        /// <returns></returns>
        public RouteConfig ToRouteConfig()
        {
            return new RouteConfig
            {
                RouteId = this.Domain,
                ClusterId = this.Domain,
                Match = new RouteMatch
                {
                    Hosts = new List<string> { this.Domain }
                }
            };
        }

        /// <summary>
        /// 发送创建反向连接指令
        /// </summary> 
        /// <param name="connectionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendCreateReverseConnectionAsync(uint connectionId, CancellationToken cancellationToken)
        {
            var channelIdBuffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(channelIdBuffer, connectionId);
            return this.webSocket.SendAsync(channelIdBuffer, WebSocketMessageType.Binary, true, cancellationToken);
        }

        /// <summary>
        /// 等待关闭
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task WaitingCloseAsync(CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4);
            try
            {
                while (cancellationToken.IsCancellationRequested == false)
                {
                    await this.webSocket.ReceiveAsync(buffer, cancellationToken);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// 由于异常而关闭
        /// </summary> 
        /// <param name="error">异常原因</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CloseAsync(string error, CancellationToken cancellationToken = default)
        {
            try
            {
                await this.webSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, error, cancellationToken);
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            return $"{this.Domain}->{this.Upstream}";
        }
    }
}
