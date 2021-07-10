using System.Linq;
using Yarp.ReverseProxy.Configuration;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 表示内存配置提供者
    /// </summary>
    sealed class MomoryConfigProvider : IProxyConfigProvider
    {
        private volatile MemoryConfig config = new();
        private readonly IRouteConfigProvider routeConfigProvider;
        private readonly IClusterConfigProvider clusterConfigProvider;

        /// <summary>
        /// 内存配置提供者
        /// </summary>
        /// <param name="httpMouseClientHandler"></param>
        /// <param name="routeConfigProvider"></param>
        /// <param name="clusterConfigProvider"></param> 
        public MomoryConfigProvider(
            IHttpMouseClientHandler httpMouseClientHandler,
            IRouteConfigProvider routeConfigProvider,
            IClusterConfigProvider clusterConfigProvider)
        {
            httpMouseClientHandler.ClientsChanged += HttpMouseClientsChanged;
            this.routeConfigProvider = routeConfigProvider;
            this.clusterConfigProvider = clusterConfigProvider;
        }

        /// <summary>
        /// 客户端变化后
        /// </summary>
        /// <param name="clients"></param>
        private void HttpMouseClientsChanged(IHttpMouseClient[] clients)
        {
            var oldConfig = this.config;

            var routes = clients.Select(item => this.routeConfigProvider.Create(item)).ToArray();
            var clusters = clients.Select(item => this.clusterConfigProvider.Create(item)).ToArray();
            this.config = new MemoryConfig(routes, clusters);

            oldConfig.SignalChange();
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <returns></returns>
        public IProxyConfig GetConfig()
        {
            return this.config;
        }
    }
}
