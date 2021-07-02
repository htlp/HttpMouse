using HttpMouse.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMouse.ClientHost
{
    sealed class HttpMouseClientHostedService : BackgroundService
    {
        private readonly IHttpMouseClient httpMouseClient;
        private readonly ILogger<HttpMouseClientHostedService> logger;

        public HttpMouseClientHostedService(
            IHttpMouseClient httpMouseClient,
            ILogger<HttpMouseClientHostedService> logger)
        {
            this.httpMouseClient = httpMouseClient;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    this.logger.LogInformation("传输ing..");
                    await this.httpMouseClient.TransportAsync(stoppingToken);
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
