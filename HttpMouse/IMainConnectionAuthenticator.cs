using System.Threading.Tasks;

namespace HttpMouse.Abstractions
{
    /// <summary>
    /// 主连接认证者
    /// </summary>
    public interface IMainConnectionAuthenticator
    {
        /// <summary>
        /// 认证
        /// </summary>
        /// <param name="clientDomain">客户端绑定的域名</param>
        /// <param name="key">客户端输入的密钥</param>
        /// <returns></returns>
        ValueTask<bool> AuthenticateAsync(string clientDomain, string? key);
    }
}
