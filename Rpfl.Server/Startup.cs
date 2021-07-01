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
        /// <param name="connectionService"></param>
        public void Configure(IApplicationBuilder app, HttpProxyService httpProxyService, ConnectionService connectionService)
        {
            app.UseSerilogRequestLogging();

            app.UseWebSockets();
            app.Use(connectionService.OnConnectedAsync);
             
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/{**catch-all}", httpProxyService.ProxyAsync);
            });
        }
    }
}
