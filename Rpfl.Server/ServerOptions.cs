using System.Net;

namespace Rpfl.Server
{
    sealed class ServerOptions
    {
        /// <summary>
        /// 密钥
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// 监听
        /// </summary>
        public ServerListen Listen { get; set; } = new ServerListen();

        /// <summary>
        /// 错误返回
        /// </summary>
        public ServerError Error { get; set; } = new ServerError();

        /// <summary>
        /// 监听选项
        /// </summary> 
        public class ServerListen
        {
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

        /// <summary>
        /// 错误返回
        /// </summary>
        public class ServerError
        {
            /// <summary>
            /// 状态码
            /// </summary>
            public int StatusCode { get; set; } = 503;

            /// <summary>
            /// 内容类型
            /// </summary>
            public string ContentType { get; set; } = "application/problem+json";

            /// <summary>
            /// 内容文件路径 
            /// </summary>
            public string ContentFile { get; set; } = "problem.json";
        }
    }
}
