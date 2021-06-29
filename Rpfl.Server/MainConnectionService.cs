using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rpfl.Server
{
    [Service(ServiceLifetime.Singleton)]
    public class MainConnectionService
    {
        private readonly ILogger<MainConnectionService> logger;

        public MainConnectionService(ILogger<MainConnectionService> logger)
        {
            this.logger = logger;
        }

        public Task CreateConnectionAsync(string domain, uint connectionId)
        {
            this.logger.LogInformation($"create connectionId:{connectionId}");
            return Task.CompletedTask;
        }
    }
}
