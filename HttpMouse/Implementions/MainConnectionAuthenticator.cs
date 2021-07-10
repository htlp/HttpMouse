using HttpMouse.Abstractions;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 主连接认证者
    /// </summary>
    sealed class MainConnectionAuthenticator : IMainConnectionAuthenticator
    {
        private readonly IOptionsMonitor<HttpMouseOptions> options;

        /// <summary>
        /// 主连接认证者
        /// </summary>
        /// <param name="options"></param>
        public MainConnectionAuthenticator(IOptionsMonitor<HttpMouseOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 认证
        /// </summary>
        /// <param name="clientDomain">客户端绑定的域名</param>
        /// <param name="key">客户端输入的密钥</param>
        /// <returns></returns>
        public ValueTask<bool> AuthenticateAsync(string clientDomain, string? key)
        {
            var serverKey = this.options.CurrentValue.Key;
            var result = serverKey == null || serverKey == key;
            return ValueTask.FromResult(result);
        }
    }
}
