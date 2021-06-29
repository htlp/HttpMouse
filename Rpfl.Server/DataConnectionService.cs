using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Sockets;
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

            var domain = context.DnsEndPoint.Host;
            await this.mainConnectionService.CreateConnectionAsync(domain, connectionId);

            try
            {
                return await source.Task;
            }
            finally
            {
                this.connectionTable.TryRemove(connectionId, out _);
            }
        }

        public async Task OnConnectedAsync(Socket socket)
        {
            using var cancellationTokenSource = new CancellationTokenSource(this.timeout);
            var networkStream = new NetworkStream(socket, ownsSocket: true);
            var pipeReader = PipeReader.Create(networkStream);

            var result = await pipeReader.ReadAsync(cancellationTokenSource.Token);
            if (result.IsCanceled ||
                result.IsCompleted ||
                TryReadConnectionId(result.Buffer, out var connectionId) == false)
            {
                socket.Dispose();
                return;
            }

            if (this.connectionTable.TryRemove(connectionId, out var value))
            {
                var position = result.Buffer.GetPosition(sizeof(uint));
                pipeReader.AdvanceTo(position);
                value.TrySetResult(networkStream);
            }
            else
            {
                pipeReader.AdvanceTo(result.Buffer.End);
            }
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

    }
}
