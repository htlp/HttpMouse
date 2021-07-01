using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Client
{
    /// <summary>
    /// 客户端接口
    /// </summary>
    public interface IRpflClient
    {
        /// <summary>
        /// 传输数据
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        Task TransportAsync(CancellationToken stoppingToken);
    }
}
