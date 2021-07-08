using HttpMouse.Connections;
using HttpMouse.HttpForwarders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HttpMouse
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
                .AddSingleton<HttpClientFactory>()
                .AddSingleton<MainConnectionService>()
                .AddSingleton<ReverseConnectionService>()
                .AddSingleton<HttpForwarderService>();

            services
                .AddOptions<HttpMouseOptions>()
                .Bind(this.Configuration.GetSection("HttpMouse"));
        }

        /// <summary>
        /// 配置中间件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="mainConnectionService"></param>
        /// <param name="httpForwarderService"></param> 
        public void Configure(IApplicationBuilder app, IHostEnvironment hostEnvironment, MainConnectionService mainConnectionService, HttpForwarderService httpForwarderService)
        {
            if (hostEnvironment.IsDevelopment())
            {
                app.UseSerilogRequestLogging();
            }

            app.UseWebSockets();
            app.Use(mainConnectionService.OnConnectedAsync);
            app.Use(httpForwarderService.SendAsync);
        }
    }
}
