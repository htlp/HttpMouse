using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 默认的集群配置提供者
    /// </summary>
    public class DefaultHttpMouseClusterProvider : IHttpMouseClusterProvider
    {
        private readonly IOptionsMonitor<HttpMouseOptions> options;

        /// <summary>
        /// 集群配置提供者
        /// </summary>
        /// <param name="options"></param>
        public DefaultHttpMouseClusterProvider(IOptionsMonitor<HttpMouseOptions> options)
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

            var clusterConfig = new ClusterConfig
            {
                ClusterId = domain,
                Destinations = destinations
            };

            var opt = this.options.CurrentValue;
            if (opt.Clusters.TryGetValue(domain, out var setting) == false)
            {
                setting = opt.DefaultCluster;
            }

            return clusterConfig with
            {
                HttpRequest = setting.HttpRequest,
                HttpClient = setting.HttpClient
            };
        }
    }
}
