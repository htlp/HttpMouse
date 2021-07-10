using System;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse
{
    /// <summary>
    /// 定义主连接
    /// </summary>
    public interface IMainConnection
    {
        /// <summary>
        /// 获取绑定的域名
        /// </summary>
        string Domain { get; }

        /// <summary>
        /// 获取上游地址
        /// </summary>
        Uri Upstream { get; } 

        /// <summary>
        /// 发送创建反向连接指令
        /// </summary> 
        /// <param name="connectionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendCreateReverseConnectionAsync(uint connectionId, CancellationToken cancellationToken);

        /// <summary>
        /// 由于异常而关闭
        /// </summary> 
        /// <param name="error">异常原因</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CloseAsync(string error, CancellationToken cancellationToken = default);

        /// <summary>
        /// 等待关闭
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WaitingCloseAsync(CancellationToken cancellationToken = default);
    }
}
