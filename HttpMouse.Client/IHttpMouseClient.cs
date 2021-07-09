using System;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Client
{
    /// <summary>
    /// 客户端接口
    /// </summary>
    public interface IHttpMouseClient : IDisposable
    {
        /// <summary>
        /// 获取是否连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 传输数据
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task TransportAsync(CancellationToken cancellationToken);
    }
}
