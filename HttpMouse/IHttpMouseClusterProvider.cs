using Yarp.ReverseProxy.Configuration;

namespace HttpMouse
{
    /// <summary>
    /// 集群配置提供者
    /// </summary>
    public interface IHttpMouseClusterProvider
    {
        /// <summary>
        /// 创建集群
        /// </summary>
        /// <param name="httpMouseClient"></param>
        /// <returns></returns>
        ClusterConfig Create(IHttpMouseClient httpMouseClient);
    }
}
