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
        private readonly IHttpMouseClientFactory httpMouseClientFactory;
        private readonly ILogger<HttpMouseClientHostedService> logger;

        public HttpMouseClientHostedService(
            IHttpMouseClientFactory httpMouseClientFactory,
            ILogger<HttpMouseClientHostedService> logger)
        {
            this.httpMouseClientFactory = httpMouseClientFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    using var client = await this.httpMouseClientFactory.CreateAsync(stoppingToken);
                    this.logger.LogInformation($"连接到服务器成功");

                    this.logger.LogInformation($"等待数据传输..");
                    await client.TransportAsync(stoppingToken);
                    this.logger.LogInformation($"数据传输结束");
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
