using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rpfl.Client.App
{
    sealed class RpflClientHostedService : BackgroundService
    {
        private readonly IRpflClient rpflClient;
        private readonly ILogger<RpflClientHostedService> logger;

        public RpflClientHostedService(
            IRpflClient rpflClient,
            ILogger<RpflClientHostedService> logger)
        {
            this.rpflClient = rpflClient;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    this.logger.LogInformation("传输ing..");
                    await this.rpflClient.TransportAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(5d), stoppingToken);
                }
            }
        }
    }
}
