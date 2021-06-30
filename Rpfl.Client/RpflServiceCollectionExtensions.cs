using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Rpfl.Client
{
    public static class RpflServiceCollectionExtensions
    {
        public static OptionsBuilder<RpflOptions> AddRpfl(this IServiceCollection services)
        {
            return services 
                .AddLogging()
                .AddHostedService<RpflClientHostedService>()
                .AddOptions<RpflOptions>();
        } 
    }
}
