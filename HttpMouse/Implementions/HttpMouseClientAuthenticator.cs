using HttpMouse.Abstractions;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 客户端认证者
    /// </summary>
    sealed class HttpMouseClientAuthenticator : IHttpMouseClientAuthenticator
    {
        private readonly IOptionsMonitor<HttpMouseOptions> options;

        /// <summary>
        /// 客户端认证者
        /// </summary>
        /// <param name="options"></param>
        public HttpMouseClientAuthenticator(IOptionsMonitor<HttpMouseOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 认证
        /// </summary>
        /// <param name="httpMouseClient">客户端</param>
        /// <returns></returns>
        public ValueTask<bool> AuthenticateAsync(IHttpMouseClient httpMouseClient)
        {
            var serverKey = this.options.CurrentValue.Key;
            var result = serverKey == null || serverKey == httpMouseClient.Key;
            return ValueTask.FromResult(result);
        }
    }
}
