using Microsoft.Extensions.DependencyInjection;
using System;

namespace HttpMouse.Client
{
    public static class HttpMouseClientServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpMouseClient(this IServiceCollection services, Action<HttpMouseClientOptions> configureOptions)
        {
            return services
                .AddHttpMouseClient()
                .Configure(configureOptions);
        }

        public static IServiceCollection AddHttpMouseClient(this IServiceCollection services)
        {
            services
                .AddOptions<HttpMouseClientOptions>()
                .ValidateDataAnnotations();

            return services
                .AddLogging()
                .AddSingleton<IHttpMouseClient, HttpMouseClient>();
        }
    }
}
