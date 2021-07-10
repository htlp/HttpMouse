using Yarp.ReverseProxy.Configuration;

namespace HttpMouse
{
    /// <summary>
    /// 路由配置提供者
    /// </summary>
    public interface IRouteConfigProvider
    {
        /// <summary>
        /// 创建路由
        /// </summary>
        /// <param name="mainConnection"></param>
        /// <returns></returns>
        RouteConfig Create(IHttpMouseClient mainConnection);
    }
}
