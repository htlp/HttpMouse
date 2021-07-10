using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 客户端
    /// </summary>
    sealed class HttpMouseClient : IHttpMouseClient
    {
        private readonly WebSocket webSocket;

        /// <summary>
        /// 获取绑定的域名
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// 获取上游地址
        /// </summary>
        public Uri Upstream { get; }

        /// <summary>
        /// 获取输入的秘钥
        /// </summary>
        public string? Key { get; }

        /// <summary>
        /// 基于websocket的主连接
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="upstream"></param>
        /// <param name="key"></param>
        /// <param name="webSocket"></param> 
        public HttpMouseClient(string domain, Uri upstream, string? key, WebSocket webSocket)
        {
            this.Domain = domain;
            this.Upstream = upstream;
            this.Key = key;
            this.webSocket = webSocket;
        }

        /// <summary>
        /// 发送创建反向连接指令
        /// </summary> 
        /// <param name="connectionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task SendCreateConnectionAsync(uint connectionId, CancellationToken cancellationToken)
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
