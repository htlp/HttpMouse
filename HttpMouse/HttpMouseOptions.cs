using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse
{
    /// <summary>
    /// HttpMouse选项
    /// </summary>
    public class HttpMouseOptions
    {
        /// <summary>
        /// 缺省的密钥
        /// </summary>
        public string? DefaultKey { get; set; }

        /// <summary>
        /// 缺省的路由设置
        /// </summary>
        public RouteSetting DefaultRoute { get; set; } = new();

        /// <summary>
        /// 缺省的集群设备
        /// </summary>
        public ClusterSetting DefaultCluster { get; set; } = new();


        /// <summary>
        /// 客户端域名的秘钥配置
        /// </summary>
        public Dictionary<string, string> Keys { get; set; } = new();

        /// <summary>
        /// 客户端域名的路由配置
        /// </summary>
        public Dictionary<string, RouteSetting> Routes { get; set; } = new();

        /// <summary>
        /// 客户端域名的集群配置
        /// </summary>
        public Dictionary<string, ClusterSetting> Clusters { get; set; } = new();

        /// <summary>
        /// 路由设置
        /// </summary>
        public class RouteSetting
        {
            /// <summary>
            /// 跨域策略
            /// </summary>
            public string? CorsPolicy { get; set; }

            /// <summary>
            /// 认证策略
            /// </summary>
            public string? AuthorizationPolicy { get; set; }
        }

        /// <summary>
        /// 集群配置
        /// </summary>
        public class ClusterSetting
        {
            /// <summary>
            /// http客户端配置
            /// </summary>
            public HttpClientConfig? HttpClient { get; set; }

            /// <summary>
            /// 转发请求配置
            /// </summary>
            public ForwarderRequestConfig? HttpRequest { get; set; }
        }
    }
}
