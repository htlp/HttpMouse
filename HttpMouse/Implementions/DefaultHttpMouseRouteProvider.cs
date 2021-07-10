using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 默认的路由配置提供者
    /// </summary>
    public class DefaultHttpMouseRouteProvider : IHttpMouseRouteProvider
    {
        /// <summary>
        /// 创建路由
        /// </summary>
        /// <param name="httpMouseClient"></param>
        /// <returns></returns>
        public virtual RouteConfig Create(IHttpMouseClient httpMouseClient)
        {
            var domain = httpMouseClient.Domain;
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
