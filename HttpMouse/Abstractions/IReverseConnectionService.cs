using Microsoft.AspNetCore.Connections;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse
{
    /// <summary>
    /// 返回连接服务接口
    /// </summary>
    public interface IReverseConnectionService
    {
        /// <summary>
        /// 创建一个反向连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        ValueTask<Stream> CreateReverseConnectionAsync(SocketsHttpConnectionContext context, CancellationToken cancellation);

        /// <summary>
        /// 处理kestrel的连接 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        Task HandleKestrelConnectionAsync(ConnectionContext context, Func<Task> next);
    }
}
