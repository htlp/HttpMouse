using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Rpfl.Client
{
    public static class RpflServiceCollectionExtensions
    {
        public static OptionsBuilder<RpflOptions> AddRpfl(this IServiceCollection services)
        {
            return services
                .AddSocketConnectionFactory()
                .AddHostedService<RpflClientHostedService>()
                .AddOptions<RpflOptions>();
        }

        /// <summary>
        /// 注册SocketConnectionFactory为IConnectionFactory
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddSocketConnectionFactory(this IServiceCollection services)
        {
            const string typeName = "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.SocketConnectionFactory";
            var factoryType = typeof(SocketTransportOptions).Assembly.GetType(typeName);
            return factoryType == null
                ? throw new NotSupportedException($"找不到类型{typeName}")
                : services.AddSingleton(typeof(IConnectionFactory), factoryType);
        }
    }
}
