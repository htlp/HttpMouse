using Microsoft.Extensions.DependencyInjection;

namespace HttpMouse.Client
{
    public static class HttpMouseClientServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpMouseClient(this IServiceCollection services)
        {
            services
                .AddOptions<HttpMouseClientOptions>()
                .ValidateDataAnnotations();

            return services
                .AddLogging()
                .AddSingleton<IHttpMouseClientFactory, HttpMouseClientFactory>();
        }
    }
}
