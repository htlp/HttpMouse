using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Connections
{
    /// <summary>
    /// 表示反向连接服务
    /// </summary>
    sealed class ReverseConnectionService
    {
        private uint _reverseConnectionId = 0;
        private readonly HttpRequestOptionsKey<string> clientDomainKey = new("ClientDomain");
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10d);
        private readonly ConcurrentDictionary<uint, IAwaitableCompletionSource<Stream>> reverseConnectAwaiterTable = new();

        private readonly MainConnectionService mainConnectionService;
        private readonly ILogger<ReverseConnectionService> logger;

        /// <summary>
        /// 反向连接服务
        /// </summary>
        /// <param name="mainConnectionService"></param>
        /// <param name="logger"></param>
        public ReverseConnectionService(
            MainConnectionService mainConnectionService,
            ILogger<ReverseConnectionService> logger)
        {
            this.mainConnectionService = mainConnectionService;
            this.logger = logger;
        }

        /// <summary>
        /// 创建一个反向连接
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async ValueTask<Stream> CreateReverseConnectionAsync(SocketsHttpConnectionContext context, CancellationToken cancellation)
        {
            if (context.InitialRequestMessage.Options.TryGetValue(clientDomainKey, out var clientDomain) == false)
            {
                throw new InvalidOperationException("无法创建http连接：未知道目标域名");
            }

            var reverseConnectionId = Interlocked.Increment(ref this._reverseConnectionId);
            using var reverseConnectionAwaiter = AwaitableCompletionSource.Create<Stream>();
            reverseConnectionAwaiter.TrySetExceptionAfter(new TimeoutException($"创建http连接{reverseConnectionId}超时"), this.timeout);
            this.reverseConnectAwaiterTable.TryAdd(reverseConnectionId, reverseConnectionAwaiter);

            try
            {
                await this.mainConnectionService.SendCreateTransportChannelAsync(clientDomain, reverseConnectionId, cancellation);
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
        /// kestrel收到连接
        /// 从kestrel连接过滤HttpConnection
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task OnKestrelConnectedAsync(ConnectionContext context, Func<Task> next)
        {
            using var cancellationTokenSource = new CancellationTokenSource(this.timeout);
            var cancellationToken = cancellationTokenSource.Token;
            var pipeReader = context.Transport.Input;
            ReadResult result;
            try
            {
                result = await pipeReader.ReadAsync(cancellationToken);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                await next();
                return;
            }

            if (result.IsCanceled || result.IsCompleted)
            {
                context.Abort();
                return;
            }

            if (TryReadReverseConnectionId(result.Buffer, out var reverseConnectionId) == false ||
                this.reverseConnectAwaiterTable.TryRemove(reverseConnectionId, out var reverseConnectionAwaiter) == false)
            {
                pipeReader.AdvanceTo(result.Buffer.Start);
                await next();
                return;
            }

            var position = result.Buffer.GetPosition(sizeof(uint));
            pipeReader.AdvanceTo(position);

            var reverseConnection = new ReverseConnection(context);
            reverseConnectionAwaiter.TrySetResult(reverseConnection);

            using var closedAwaiter = AwaitableCompletionSource.Create<object?>();
            context.ConnectionClosed.Register(state => ((IAwaitableCompletionSource)state!).TrySetResult(null), closedAwaiter);
            await closedAwaiter.Task;
            await context.DisposeAsync();
        }


        /// <summary>
        /// 读取ReverseConnection的id
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryReadReverseConnectionId(ReadOnlySequence<byte> buffer, out uint value)
        {
            var reader = new SequenceReader<byte>(buffer);
            if (reader.TryReadBigEndian(out int intValue))
            {
                value = (uint)intValue;
                return true;
            }
            value = default;
            return false;
        }
    }
}
