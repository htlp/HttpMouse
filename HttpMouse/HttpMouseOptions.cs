using System.Collections.Generic;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse
{
    /// <summary>
    /// HttpMouse选项
    /// </summary>
    public class HttpMouseOptions
    {
        /// <summary>
        /// 连接密钥
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// 客户端域名的请求配置
        /// </summary>
        public Dictionary<string, ForwarderRequestConfig> HttpRequest { get; set; } = new();

        /// <summary>
        /// 错误回退配置
        /// </summary>
        public FallbackConfig Fallback { get; set; } = new FallbackConfig();

        /// <summary>
        /// 错误回退配置
        /// </summary>
        public class FallbackConfig
        {
            /// <summary>
            /// 响应状态码
            /// </summary>
            public int StatusCode { get; set; } = 503;

            /// <summary>
            /// 响应内容类型
            /// </summary>
            public string ContentType { get; set; } = "application/problem+json";

            /// <summary>
            /// 响应内容文件路径 
            /// </summary>
            public string ContentFile { get; set; } = "fallback.json";
        }
    }
}
