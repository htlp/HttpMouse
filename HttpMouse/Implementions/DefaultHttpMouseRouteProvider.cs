using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Yarp.ReverseProxy.Configuration;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 默认的路由配置提供者
    /// </summary>
    public class DefaultHttpMouseRouteProvider : IHttpMouseRouteProvider
    {
        private IOptionsMonitor<HttpMouseOptions> options;

        /// <summary>
        /// 路由配置提供者
        /// </summary>
        /// <param name="options"></param>
        public DefaultHttpMouseRouteProvider(IOptionsMonitor<HttpMouseOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// 创建路由
        /// </summary>
        /// <param name="httpMouseClient"></param>
        /// <returns></returns>
        public virtual RouteConfig Create(IHttpMouseClient httpMouseClient)
        {
            var domain = httpMouseClient.Domain;
            var routeConfig = new RouteConfig
            {
                RouteId = domain,
                ClusterId = domain,
                Match = new RouteMatch
                {
                    Hosts = new List<string> { domain }
                }
            };

            var opt = this.options.CurrentValue;
            if (opt.Routes.TryGetValue(domain, out var setting) == false)
            {
                setting = opt.DefaultRoute;
            }

            return routeConfig with
            {
                CorsPolicy = setting.CorsPolicy,
                AuthorizationPolicy = setting.AuthorizationPolicy
            };
        }
    }
}
