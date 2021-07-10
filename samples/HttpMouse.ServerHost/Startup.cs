using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace HttpMouse.ServerHost
{
    sealed class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// 配置服务
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpMouse(this.Configuration.GetSection("HttpMouse"));
            services.Configure<FallbackOptions>(this.Configuration.GetSection("Fallback"));
        }

        /// <summary>
        /// 配置中间件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="hostEnvironment"></param>
        public void Configure(IApplicationBuilder app, IHostEnvironment hostEnvironment)
        {
            app.UseHttpMouse();

            if (hostEnvironment.IsDevelopment())
            {
                app.UseSerilogRequestLogging();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapReverseProxy();

                endpoints.MapFallback(context =>
                {
                    var fallback = context.RequestServices.GetRequiredService<IOptionsMonitor<FallbackOptions>>().CurrentValue;
                    context.Response.StatusCode = fallback.StatusCode;
                    context.Response.ContentType = fallback.ContentType;
                    return context.Response.SendFileAsync(fallback.ContentFile);
                });
            });
        }
    }
}
