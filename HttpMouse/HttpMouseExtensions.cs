using HttpMouse.Implementions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace HttpMouse
{
    static class HttpMouseExtensions
    {
        /// <summary>
        /// 添加HttpMouse
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpMouse(this IServiceCollection services)
        {
            services
               .AddReverseProxy()
               .AddClientDomainOptionsTransform();

            return services
                .AddSingleton<IProxyConfigProvider, MomoryConfigProvider>()
                .AddSingleton<IMainConnectionService, MainConnectionService>()
                .AddSingleton<IReverseConnectionService, ReverseConnectionService>()
                .AddSingleton<IForwarderHttpClientFactory, ReverseForwarderHttpClientFactory>();
        }

        /// <summary>
        /// 配置HttpMouse
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigureHttpMouse(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<HttpMouseOptions>(configuration);
        }


        /// <summary>
        /// 添加ClientDomain的options
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static IReverseProxyBuilder AddClientDomainOptionsTransform(this IReverseProxyBuilder builder)
        {
            var optionsKey = new HttpRequestOptionsKey<string>("ClientDomain");
            return builder.AddTransforms(ctx => ctx.AddRequestTransform(request =>
            {
                var clientDomain = request.HttpContext.Request.Host.Host;
                request.ProxyRequest.Options.Set(optionsKey, clientDomain);
                return ValueTask.CompletedTask;
            }));
        }

        /// <summary>
        /// 映射反向代理回退
        /// </summary>
        /// <param name="endpoints"></param>
        public static void MapReverseProxyFallback(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapFallback(context =>
            {
                var fallback = context.RequestServices.GetRequiredService<IOptionsMonitor<HttpMouseOptions>>().CurrentValue.Fallback;
                context.Response.StatusCode = fallback.StatusCode;
                context.Response.ContentType = fallback.ContentType;
                return context.Response.SendFileAsync(fallback.ContentFile);
            });
        }
    }
}
