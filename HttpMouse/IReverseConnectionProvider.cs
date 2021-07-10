using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse
{
    /// <summary>
    /// 反向连接提供者
    /// </summary>
    public interface IReverseConnectionProvider
    {
        /// <summary>
        /// 创建一个反向连接
        /// </summary>
        /// <param name="clientDomain">客户端域名</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask<Stream> CreateAsync(string clientDomain, CancellationToken cancellationToken);

        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        Task HandleConnectionAsync(HttpContext context, Func<Task> next);
    }
}
