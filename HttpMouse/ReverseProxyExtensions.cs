using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Transforms;

namespace HttpMouse
{
    static class ReverseProxyExtensions
    {
        /// <summary>
        /// 添加ClientDomain的options
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IReverseProxyBuilder AddClientDomainOptionsTransform(this IReverseProxyBuilder builder)
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
