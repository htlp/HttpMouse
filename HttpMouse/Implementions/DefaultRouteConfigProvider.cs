using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 默认的路由配置提供者
    /// </summary>
    public class DefaultRouteConfigProvider : IRouteConfigProvider
    {
        /// <summary>
        /// 创建路由
        /// </summary>
        /// <param name="mainConnection"></param>
        /// <returns></returns>
        public virtual RouteConfig Create(IHttpMouseClient mainConnection)
        {
            var domain = mainConnection.Domain;
            return new RouteConfig
            {
                RouteId = domain,
                ClusterId = domain,
                Match = new RouteMatch
                {
                    Hosts = new List<string> { domain }
                }
            };
        }
    }
}
