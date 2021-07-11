using HttpMouse;
using HttpMouse.Implementions;
using Microsoft.AspNetCore.Builder;
using System;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// HttpMouse的中间件扩展
    /// </summary>
    public static class HttpMouseApplicationBuilderExtensions
    {
        /// <summary>
        /// 使用httpMouse中间件
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttpMouse(this IApplicationBuilder builder)
        {
            builder.EnsureImplementation<IProxyConfigProvider, HttpMouseProxyConfigProvider>();
            builder.EnsureImplementation<IForwarderHttpClientFactory, HttpMouseForwarderHttpClientFactory>();

            var httpMouseClientHandler = builder.ApplicationServices.GetRequiredService<IHttpMouseClientHandler>();
            var reverseConnectionHandler = builder.ApplicationServices.GetRequiredService<IReverseConnectionHandler>();

            builder.UseWebSockets();
            builder.Use(httpMouseClientHandler.HandleConnectionAsync);
            builder.Use(reverseConnectionHandler.HandleConnectionAsync);

            return builder;
        }

        /// <summary>
        /// 确保服务的实现类型为指定类型
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="builder"></param>
        private static void EnsureImplementation<TService, TImplementation>(this IApplicationBuilder builder) where TService : notnull
        {
            if (builder.ApplicationServices.GetRequiredService<TService>() is not TImplementation)
            {
                throw new InvalidOperationException($"不允许替换{typeof(TService)}的实现类");
            }
        }
    }
}
