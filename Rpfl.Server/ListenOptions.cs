using System.Net;

namespace Rpfl.Server
{
    /// <summary>
    /// 监听选项
    /// </summary> 
    sealed class ListenOptions
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// http
        /// </summary>
        public HttpEndPoint? Http { get; set; }

        /// <summary>
        /// https
        /// </summary>
        public HttpsEndPoint? Https { get; set; }

        /// <summary>
        /// http节点
        /// </summary>
        public class HttpEndPoint
        {
            /// <summary>
            /// ip地址
            /// </summary>
            public IPAddress IPAddress { get; set; } = IPAddress.IPv6Any;

            /// <summary>
            /// 端口
            /// </summary>
            public virtual int Port { get; set; } = 80;
        }

        /// <summary>
        /// https节点
        /// </summary>
        public class HttpsEndPoint : HttpEndPoint
        {
            /// <summary>
            /// 端口
            /// </summary>
            public override int Port { get; set; } = 443;

            /// <summary>
            /// 证书
            /// </summary>
            public Certificate Certificate { get; set; } = new Certificate();
        }

        /// <summary>
        /// 证书
        /// </summary>
        public class Certificate
        {
            /// <summary>
            /// pfx路径
            /// </summary>
            public string Path { get; set; } = string.Empty;

            /// <summary>
            /// pfx密码
            /// </summary>
            public string? Password { get; set; }
        }
    }
}
