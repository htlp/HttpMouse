using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HttpMouse
{
    static class FallbackExtensions
    {
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
