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

namespace Rpfl.Server.Applications
{
    /// <summary>
    /// 传输通道服务
    /// </summary>
    sealed class TransportChannelService
    {
        private uint _channelId = 0;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(5d);
        private readonly ConcurrentDictionary<uint, IAwaitableCompletionSource<Stream>> channelAwaiterTable = new();

        private readonly ConnectionService connectionService;
        private readonly ILogger<TransportChannelService> logger;

        /// <summary>
        /// 传输通道服务
        /// </summary>
        /// <param name="connectionService"></param>
        /// <param name="logger"></param>
        public TransportChannelService(
            ConnectionService connectionService,
            ILogger<TransportChannelService> logger)
        {
            this.connectionService = connectionService;
            this.logger = logger;
        }

        /// <summary>
        /// 创建一个传输通道
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async ValueTask<Stream> CreateChannelAsync(SocketsHttpConnectionContext context, CancellationToken cancellation)
        {
            var channelId = Interlocked.Increment(ref this._channelId);
            using var channelAwaiter = AwaitableCompletionSource.Create<Stream>();
            channelAwaiter.TrySetExceptionAfter(new TimeoutException($"创建传输通道{channelId}超时"), this.timeout);
            this.channelAwaiterTable.TryAdd(channelId, channelAwaiter);

            try
            {
                this.logger.LogInformation($"正在创建传输通道{channelId}");
                var key = new HttpRequestOptionsKey<string>("ClientDomain");
                context.InitialRequestMessage.Options.TryGetValue<string>(key, out var clientDomain);
                await this.connectionService.SendCreateTransportChannelAsync(clientDomain!, channelId, cancellation);
                var channel = await channelAwaiter.Task;
                this.logger.LogInformation($"创建{clientDomain}的传输通道{channelId}成功");
                return channel;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.Message);
                throw;
            }
            finally
            {
                this.channelAwaiterTable.TryRemove(channelId, out _);
            }
        }

        /// <summary>
        /// 收到连接
        /// 从连接查找传输通道
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task OnConnectedAsync(ConnectionContext context, Func<Task> next)
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

            if (TryReadChannelId(result.Buffer, out var connectionId) == false ||
                this.channelAwaiterTable.TryRemove(connectionId, out var channelAwaiter) == false)
            {
                pipeReader.AdvanceTo(result.Buffer.Start);
                await next();
                return;
            }

            var position = result.Buffer.GetPosition(sizeof(uint));
            pipeReader.AdvanceTo(position);

            var channel = new TransportChannel(context);
            channelAwaiter.TrySetResult(channel);

            using var closedAwaiter = AwaitableCompletionSource.Create<object?>();
            context.ConnectionClosed.Register(state => ((IAwaitableCompletionSource)state!).TrySetResult(null), closedAwaiter);
            await closedAwaiter.Task;
            await context.DisposeAsync();
        }


        /// <summary>
        /// 读取通道id
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryReadChannelId(ReadOnlySequence<byte> buffer, out uint value)
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


        /// <summary>
        /// 传输通道
        /// </summary>
        private class TransportChannel : Stream
        {
            private readonly ConnectionContext context;
            private readonly Stream readStream;
            private readonly Stream wirteStream;

            public TransportChannel(ConnectionContext context)
            {
                this.context = context;
                this.readStream = context.Transport.Input.AsStream();
                this.wirteStream = context.Transport.Output.AsStream();
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                this.wirteStream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return this.wirteStream.FlushAsync(cancellationToken);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return this.readStream.Read(buffer, offset, count);
            }
            public override void Write(byte[] buffer, int offset, int count)
            {
                this.wirteStream.Write(buffer, offset, count);
            }
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return this.readStream.ReadAsync(buffer, cancellationToken);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return this.readStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                this.wirteStream.Write(buffer);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return this.wirteStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await this.wirteStream.WriteAsync(buffer, cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                this.context.Abort();
            }

            public override ValueTask DisposeAsync()
            {
                this.context.Abort();
                return ValueTask.CompletedTask;
            }
        }
    }
}
