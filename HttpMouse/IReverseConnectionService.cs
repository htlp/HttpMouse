using Microsoft.AspNetCore.Http;
using System;
using System.IO;
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
        /// <param name="clientDomain">客户端域名</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        ValueTask<Stream> CreateReverseConnectionAsync(string clientDomain, CancellationToken cancellation);

        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        Task HandleConnectionAsync(HttpContext context, Func<Task> next);
    }
}
