using HttpMouse;
using Microsoft.AspNetCore.Builder;

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
            var httpMouseClientHandler = builder.ApplicationServices.GetRequiredService<IHttpMouseClientHandler>();
            var reverseConnectionHandler = builder.ApplicationServices.GetRequiredService<IReverseConnectionHandler>();

            builder.UseWebSockets();
            builder.Use(httpMouseClientHandler.HandleConnectionAsync);
            builder.Use(reverseConnectionHandler.HandleConnectionAsync);

            return builder;
        }
    }
}
