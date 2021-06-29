using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Server
{
    public class DataConnectionHostedService : BackgroundService
    {
        private readonly Socket socket;
        private readonly DataConnectionService dataConnectionService;

        public DataConnectionHostedService(DataConnectionService dataConnectionService)
        {
            this.dataConnectionService = dataConnectionService;
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            this.socket.Bind(new IPEndPoint(IPAddress.Any, 8080));
            this.socket.Listen();

            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                var socket = await this.socket.AcceptAsync();
                await this.dataConnectionService.OnConnectedAsync(socket);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.socket.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}
