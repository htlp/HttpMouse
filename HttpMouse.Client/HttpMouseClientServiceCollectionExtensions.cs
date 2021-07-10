using HttpMouse.Client.Implementions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HttpMouse.Client
{
    /// <summary>
    /// HttpMouseClient扩展
    /// </summary>
    public static class HttpMouseClientServiceCollectionExtensions
    {
        /// <summary>
        /// 添加HttpMouseClient工厂
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpMouseClient(this IServiceCollection services, Action<HttpMouseClientOptions> configureOptions)
        {
            return services.AddHttpMouseClient().Configure(configureOptions);
        }

        /// <summary>
        /// 添加HttpMouseClient工厂
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpMouseClient(this IServiceCollection services, IConfiguration configuration)
        {
            return services.AddHttpMouseClient().Configure<HttpMouseClientOptions>(configuration);
        }

        /// <summary>
        /// 添加HttpMouseClient工厂
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpMouseClient(this IServiceCollection services)
        {
            services
                .AddOptions<HttpMouseClientOptions>()
                .ValidateDataAnnotations();

            return services
                .AddSingleton<IHttpMouseClientFactory, HttpMouseClientFactory>();
        }
    }
}
