using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Client
{
    /// <summary>
    /// HttpMouseClient工厂
    /// </summary>
    public interface IHttpMouseClientFactory
    {
        /// <summary>
        /// 创建客户端实例
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        Task<IHttpMouseClient> CreateAsync(CancellationToken cancellation);
    }
}
