using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rpfl.Server.Applications;
using Serilog;

namespace Rpfl.Server
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
                .AddHttpProxy()
                .AddSingleton<HttpProxyService>()
                .AddSingleton<ConnectionService>()
                .AddSingleton<TransportChannelService>();

            services
                .AddOptions<ListenOptions>().Bind(this.Configuration.GetSection("Listen"));
        }

        /// <summary>
        /// 配置中间件
        /// </summary>
        /// <param name="app"></param> 
        /// <param name="httpProxyService"></param>
        public void Configure(IApplicationBuilder app, HttpProxyService httpProxyService)
        {
            app.UseWebSockets();
            app.Use(next => async context =>
            {
                if (context.WebSockets.IsWebSocketRequest == true)
                {
                    await context.RequestServices.GetRequiredService<ConnectionService>().OnConnectedAsync(context);
                }
                else
                {
                    await next(context);
                }
            });

            app.UseSerilogRequestLogging();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/{**catch-all}", httpProxyService.ProxyAsync);
            });
        }
    }
}
