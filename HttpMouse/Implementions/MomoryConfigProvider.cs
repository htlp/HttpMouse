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

        /// <summary>
        /// 内存配置提供者
        /// </summary>
        /// <param name="mainConnectionHandler">主连接处理者</param>
        public MomoryConfigProvider(IMainConnectionHandler mainConnectionHandler)
        {
            mainConnectionHandler.ConnectionsChanged += MainConnectionChanged;
        }

        /// <summary>
        /// 连接变化后
        /// </summary>
        /// <param name="connections"></param>
        private void MainConnectionChanged(IMainConnection[] connections)
        {
            var oldConfig = this.config;

            var routes = connections.Select(item => item.ToRouteConfig()).ToArray();
            var clusters = connections.Select(item => item.ToClusterConfig()).ToArray();
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
