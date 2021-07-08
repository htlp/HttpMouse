using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace HttpMouse
{
    /// <summary>
    /// 主连接服务接口
    /// </summary>
    public interface IMainConnectionService
    {
        /// <summary>
        /// 主连接变化后
        /// </summary>
        event Action<IMainConnection[]>? ConnectionChanged;

        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        Task HandleConnectionAsync(HttpContext context, Func<Task> next);

        /// <summary>
        /// 通过客户端绑定的域名尝试获取主连接 
        /// </summary>
        /// <param name="clientDomain">客户端绑定的域名</param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetValue(string clientDomain, [MaybeNullWhen(false)] out IMainConnection value);
    }
}