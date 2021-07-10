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
    /// 表示反向连接提值者
    /// </summary>
    sealed class ReverseConnectionProvider : IReverseConnectionProvider
    {
        private readonly IHttpMouseClientHandler mainConnectionHandler;
        private readonly ILogger<ReverseConnectionProvider> logger;

        private uint _connectionId = 0;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10d);
        private readonly ConcurrentDictionary<uint, IAwaitableCompletionSource<Stream>> connectionAwaiterTable = new();

        /// <summary>
        /// 反向连接提值者
        /// </summary>
        /// <param name="mainConnectionHandler"></param>
        /// <param name="logger"></param>
        public ReverseConnectionProvider(
            IHttpMouseClientHandler mainConnectionHandler,
            ILogger<ReverseConnectionProvider> logger)
        {
            this.mainConnectionHandler = mainConnectionHandler;
            this.logger = logger;
        }

        /// <summary>
        /// 创建一个反向连接
        /// </summary>
        /// <param name="clientDomain">客户端域名</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async ValueTask<Stream> CreateAsync(string clientDomain, CancellationToken cancellation)
        {
            if (this.mainConnectionHandler.TryGetValue(clientDomain, out var mainConnection) == false)
            {
                throw new Exception($"无法创建反向连接：上游{clientDomain}未连接");
            }

            var connectionId = Interlocked.Increment(ref this._connectionId);
            using var connectionAwaiter = AwaitableCompletionSource.Create<Stream>();
            connectionAwaiter.TrySetExceptionAfter(new TimeoutException($"创建反向连接{connectionId}超时"), this.timeout);
            this.connectionAwaiterTable.TryAdd(connectionId, connectionAwaiter);

            try
            {
                await mainConnection.SendCreateConnectionAsync(connectionId, cancellation);
                return await connectionAwaiter.Task;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                throw;
            }
            finally
            {
                this.connectionAwaiterTable.TryRemove(connectionId, out _);
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
            if (TryReadConnectionId(context, out var connectionId) == false ||
                this.connectionAwaiterTable.TryRemove(connectionId, out var connectionAwaiter) == false)
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
            connectionAwaiter.TrySetResult(reverseConnection);

            using var closedAwaiter = AwaitableCompletionSource.Create<object?>();
            lifetime.ConnectionClosed.Register(state => ((IAwaitableCompletionSource)state!).TrySetResult(null), closedAwaiter);
            await closedAwaiter.Task;
        }

        /// <summary>
        /// 读取反向连接的id
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryReadConnectionId(HttpContext context, out uint value)
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
