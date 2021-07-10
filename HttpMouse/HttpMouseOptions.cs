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
    }
}
