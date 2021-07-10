using Yarp.ReverseProxy.Configuration;

namespace HttpMouse
{
    /// <summary>
    /// 集群配置提供者
    /// </summary>
    public interface IClusterConfigProvider
    {
        /// <summary>
        /// 创建集群
        /// </summary>
        /// <param name="mainConnection"></param>
        /// <returns></returns>
        ClusterConfig Create(IHttpMouseClient mainConnection);
    }
}
