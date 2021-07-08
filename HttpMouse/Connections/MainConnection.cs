using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Connections
{
    /// <summary>
    /// 表示一个连接
    /// </summary>
    sealed class MainConnection
    {
        /// <summary>
        /// 获取绑定的域名
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// 获取上游地址
        /// </summary>
        public Uri Upstream { get; }

        /// <summary>
        /// 获取关联的websocket
        /// </summary>
        public WebSocket WebSocket { get; }

        /// <summary>
        /// 表示一个连接
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="Upstream"></param>
        /// <param name="WebSocket"></param>
        public MainConnection(string domain, Uri Upstream, WebSocket WebSocket)
        {
            this.Domain = domain;
            this.Upstream = Upstream;
            this.WebSocket = WebSocket;
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
                    await this.WebSocket.ReceiveAsync(buffer, cancellationToken);
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
                await this.WebSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, error, cancellationToken);
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
