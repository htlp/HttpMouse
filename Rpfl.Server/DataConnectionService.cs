using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Server
{
    [Service(ServiceLifetime.Singleton)]
    public class DataConnectionService
    {
        private uint _connectionId = 0;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(20);
        private readonly MainConnectionService mainConnectionService;

        private readonly ConcurrentDictionary<uint, IAwaitableCompletionSource<Stream>> connectionTable = new();

        public DataConnectionService(MainConnectionService mainConnectionService)
        {
            this.mainConnectionService = mainConnectionService;
        }

        public async ValueTask<Stream> CreateConnectionAsync(SocketsHttpConnectionContext context, CancellationToken cancellation)
        {
            var connectionId = Interlocked.Increment(ref this._connectionId);
            using var source = AwaitableCompletionSource.Create<Stream>();
            source.TrySetExceptionAfter(new Exception("创建连接超时"), this.timeout);
            this.connectionTable.TryAdd(connectionId, source);

            try
            { 
                var domain = "a.localhost";// context.DnsEndPoint.Host;
                await this.mainConnectionService.NotifyCreateDataConnectionAsync(domain, connectionId, cancellation);
                return await source.Task;
            }
            finally
            {
                this.connectionTable.TryRemove(connectionId, out _);
            }
        }

        public async Task OnConnectedAsync(ConnectionContext context, Func<Task> next)
        {
            using var cancellationTokenSource = new CancellationTokenSource(this.timeout);
            var pipeReader = context.Transport.Input;

            var result = await pipeReader.ReadAsync(cancellationTokenSource.Token);
            if (result.IsCanceled || result.IsCompleted)
            {
                context.Abort();
                return;
            }

            if (TryReadConnectionId(result.Buffer, out var connectionId) == false ||
                this.connectionTable.TryRemove(connectionId, out var streamAwaitable) == false)
            {
                pipeReader.AdvanceTo(result.Buffer.Start);
                await next();
                return;
            }

            var position = result.Buffer.GetPosition(sizeof(uint));
            pipeReader.AdvanceTo(position);
            var duplexStream = new DuplexStream(context);
            streamAwaitable.TrySetResult(duplexStream);

            using var awaitable = AwaitableCompletionSource.Create<object?>();
            context.ConnectionClosed.Register(state => ((IAwaitableCompletionSource)state!).TrySetResult(null), awaitable);
            await awaitable.Task;
            await context.DisposeAsync();
        }

        private static bool TryReadConnectionId(ReadOnlySequence<byte> buffer, out uint value)
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


        private class DuplexStream : Stream
        {
            private readonly ConnectionContext context;
            private readonly Stream readStream;
            private readonly Stream wirteStream;

            public DuplexStream(ConnectionContext context)
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
