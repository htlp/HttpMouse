using Yarp.ReverseProxy.Configuration;

namespace HttpMouse
{
    /// <summary>
    /// 路由配置提供者
    /// </summary>
    public interface IHttpMouseRouteProvider
    {
        /// <summary>
        /// 创建路由
        /// </summary>
        /// <param name="httpMouseClient"></param> 
        /// <returns></returns>
        RouteConfig Create(IHttpMouseClient httpMouseClient);
    }
}
