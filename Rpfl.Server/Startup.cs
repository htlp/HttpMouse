using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                .AddHttpForwarder()
                .AddSingleton<HttpForwarderService>()
                .AddSingleton<ConnectionService>()
                .AddSingleton<TransportChannelService>();

            services
                .AddOptions<ListenOptions>().Bind(this.Configuration.GetSection("Listen"));
        }

        /// <summary>
        /// 配置中间件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="connectionService"></param>
        /// <param name="httpForwarderService"></param> 
        public void Configure(IApplicationBuilder app, IHostEnvironment hostEnvironment, ConnectionService connectionService, HttpForwarderService httpForwarderService)
        {
            if (hostEnvironment.IsDevelopment())
            {
                app.UseSerilogRequestLogging();
            }

            app.UseWebSockets();
            app.Use(connectionService.OnConnectedAsync);
            app.Use(httpForwarderService.SendAsync);
        }
    }
}
