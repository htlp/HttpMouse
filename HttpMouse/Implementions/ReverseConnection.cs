using Microsoft.AspNetCore.Connections;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.Implementions
{
    /// <summary>
    /// 表示反向连接
    /// </summary>
    sealed class ReverseConnection : Stream
    {
        private readonly ConnectionContext context;
        private readonly Stream readStream;
        private readonly Stream wirteStream;

        /// <summary>
        /// 反向连接
        /// </summary>
        /// <param name="context">连接上下文</param>
        public ReverseConnection(ConnectionContext context)
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
