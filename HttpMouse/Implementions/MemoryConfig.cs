using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using Yarp.ReverseProxy.Configuration;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 表示内存配置
    /// </summary>
    sealed class MemoryConfig : IProxyConfig
    {
        private readonly CancellationTokenSource cancellationToken = new();

        /// <summary>
        /// 获取路由配置
        /// </summary>
        public IReadOnlyList<RouteConfig> Routes { get; }

        /// <summary>
        /// 获取集群配置
        /// </summary>
        public IReadOnlyList<ClusterConfig> Clusters { get; }

        /// <summary>
        /// 获取变化通知令牌
        /// </summary>
        public IChangeToken ChangeToken { get; }

        /// <summary>
        /// 内存配置
        /// </summary>
        public MemoryConfig()
            : this(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>())
        {
        }

        /// <summary>
        /// 内存配置
        /// </summary>
        /// <param name="routes"></param>
        /// <param name="clusters"></param>
        public MemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            this.Routes = routes;
            this.Clusters = clusters;
            this.ChangeToken = new CancellationChangeToken(cancellationToken.Token);
        }

        /// <summary>
        /// 通知配置变化
        /// </summary>
        public void SignalChange()
        {
            this.cancellationToken.Cancel();
        }
    }
}
