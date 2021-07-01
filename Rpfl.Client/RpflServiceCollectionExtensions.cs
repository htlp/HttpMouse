using Microsoft.Extensions.DependencyInjection;
using System;

namespace Rpfl.Client
{
    public static class RpflServiceCollectionExtensions
    {
        public static IServiceCollection AddRpfl(this IServiceCollection services, Action<RpflClientOptions> configureOptions)
        {
            return services
                .AddRpfl()
                .Configure(configureOptions);
        }

        public static IServiceCollection AddRpfl(this IServiceCollection services)
        {
            services
                .AddOptions<RpflClientOptions>()
                .ValidateDataAnnotations();

            return services
                .AddLogging()
                .AddSingleton<IRpflClient, RpflClient>();
        }
    }
}
