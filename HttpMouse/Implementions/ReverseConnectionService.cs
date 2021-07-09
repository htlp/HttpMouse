using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 表示反向连接服务
    /// </summary>
    sealed class ReverseConnectionService : IReverseConnectionService
    {
        private uint _reverseConnectionId = 0;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10d);
        private readonly ConcurrentDictionary<uint, IAwaitableCompletionSource<Stream>> reverseConnectAwaiterTable = new();

        private readonly IMainConnectionService mainConnectionService;
        private readonly ILogger<ReverseConnectionService> logger;

        /// <summary>
        /// 反向连接服务
        /// </summary>
        /// <param name="mainConnectionService"></param>
        /// <param name="logger"></param>
        public ReverseConnectionService(
            IMainConnectionService mainConnectionService,
            ILogger<ReverseConnectionService> logger)
        {
            this.mainConnectionService = mainConnectionService;
            this.logger = logger;
        }

        /// <summary>
        /// 创建一个反向连接
        /// </summary>
        /// <param name="clientDomain">客户端域名</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async ValueTask<Stream> CreateReverseConnectionAsync(string clientDomain, CancellationToken cancellation)
        {
            if (this.mainConnectionService.TryGetValue(clientDomain, out var mainConnection) == false)
            {
                throw new Exception($"无法创建反向连接：上游{clientDomain}未连接");
            }

            var reverseConnectionId = Interlocked.Increment(ref this._reverseConnectionId);
            using var reverseConnectionAwaiter = AwaitableCompletionSource.Create<Stream>();
            reverseConnectionAwaiter.TrySetExceptionAfter(new TimeoutException($"创建http连接{reverseConnectionId}超时"), this.timeout);
            this.reverseConnectAwaiterTable.TryAdd(reverseConnectionId, reverseConnectionAwaiter);

            try
            {
                await mainConnection.SendCreateReverseConnectionAsync(reverseConnectionId, cancellation);
                return await reverseConnectionAwaiter.Task;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                throw;
            }
            finally
            {
                this.reverseConnectAwaiterTable.TryRemove(reverseConnectionId, out _);
            }
        }


        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task HandleConnectionAsync(HttpContext context, Func<Task> next)
        {
            if (TryReadReverseConnectionId(context, out var reverseConnectionId) == false ||
                this.reverseConnectAwaiterTable.TryRemove(reverseConnectionId, out var reverseConnectionAwaiter) == false)
            {
                await next();
                return;
            }

            var lifetime = context.Features.Get<IConnectionLifetimeFeature>();
            var transport = context.Features.Get<IConnectionTransportFeature>();

            if (lifetime == null || transport == null)
            {
                await next();
                return;
            }

            using var reverseConnection = new ReverseConnection(lifetime, transport);
            reverseConnectionAwaiter.TrySetResult(reverseConnection);

            using var closedAwaiter = AwaitableCompletionSource.Create<object?>();
            lifetime.ConnectionClosed.Register(state => ((IAwaitableCompletionSource)state!).TrySetResult(null), closedAwaiter);
            await closedAwaiter.Task;
        }

        /// <summary>
        /// 读取ReverseConnection的id
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryReadReverseConnectionId(HttpContext context, out uint value)
        {
            const string method = "REVERSE";
            if (context.Request.Method != method)
            {
                value = default;
                return default;
            }

            var path = context.Request.Path.Value.AsSpan();
            return uint.TryParse(path[1..], out value);
        }
    }
}
