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
        /// <param name="mainConnectionHandler">主连接处理者</param>
        /// <param name="routeConfigProvider"></param>
        /// <param name="clusterConfigProvider"></param> 
        public MomoryConfigProvider(
            IMainConnectionHandler mainConnectionHandler,
            IRouteConfigProvider routeConfigProvider,
            IClusterConfigProvider clusterConfigProvider)
        {
            mainConnectionHandler.ConnectionsChanged += MainConnectionChanged;
            this.routeConfigProvider = routeConfigProvider;
            this.clusterConfigProvider = clusterConfigProvider;
        }

        /// <summary>
        /// 连接变化后
        /// </summary>
        /// <param name="connections"></param>
        private void MainConnectionChanged(IMainConnection[] connections)
        {
            var oldConfig = this.config;

            var routes = connections.Select(item => this.routeConfigProvider.Create(item)).ToArray();
            var clusters = connections.Select(item => this.clusterConfigProvider.Create(item)).ToArray();
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
