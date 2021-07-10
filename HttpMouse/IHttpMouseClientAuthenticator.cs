using System.Threading.Tasks;

namespace HttpMouse.Abstractions
{
    /// <summary>
    /// 客户端认证者
    /// </summary>
    public interface IHttpMouseClientAuthenticator
    {
        /// <summary>
        /// 认证
        /// </summary>
        /// <param name="httpMouseClient">客户端</param>
        /// <returns></returns>
        ValueTask<bool> AuthenticateAsync(IHttpMouseClient httpMouseClient);
    }
}
