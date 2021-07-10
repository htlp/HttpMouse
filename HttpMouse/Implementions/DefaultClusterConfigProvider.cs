using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 默认的集群配置提供者
    /// </summary>
    public class DefaultClusterConfigProvider : IClusterConfigProvider
    {
        private readonly IOptionsMonitor<HttpMouseOptions> options;

        /// <summary>
        /// 集群配置提供者
        /// </summary>
        /// <param name="options"></param>
        public DefaultClusterConfigProvider(IOptionsMonitor<HttpMouseOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 创建集群
        /// </summary>
        /// <param name="httpMouseClient"></param>
        /// <returns></returns>
        public virtual ClusterConfig Create(IHttpMouseClient httpMouseClient)
        {
            var domain = httpMouseClient.Domain;
            var address = httpMouseClient.Upstream.ToString();

            var destinations = new Dictionary<string, DestinationConfig>
            {
                [domain] = new DestinationConfig { Address = address }
            };

            if (this.options.CurrentValue.HttpRequest.TryGetValue(domain, out var httpRequest) == false)
            {
                httpRequest = ForwarderRequestConfig.Empty;
            }

            return new ClusterConfig
            {
                ClusterId = domain,
                Destinations = destinations,
                HttpRequest = httpRequest
            };
        }
    }
}
