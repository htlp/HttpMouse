using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services
                .AddHttpMouse()
                .ConfigureHttpMouse(this.Configuration.GetSection("HttpMouse"));
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
                endpoints.MapReverseProxyFallback();
            });
        }
    }
}
