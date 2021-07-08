using HttpMouse.Implementions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

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
                .AddReverseProxy()
                .AddTransforms(ctx => ctx.AddRequestTransform(request =>
                {
                    var key = new HttpRequestOptionsKey<string>("ClientDomain");
                    var clientDomain = request.HttpContext.Request.Host.Host;
                    request.ProxyRequest.Options.Set(key, clientDomain);
                    return ValueTask.CompletedTask;
                }));

            services
                .AddSingleton<IProxyConfigProvider, MomoryConfigProvider>()
                .AddSingleton<IMainConnectionService, MainConnectionService>()
                .AddSingleton<IReverseConnectionService, ReverseConnectionService>()
                .AddSingleton<IForwarderHttpClientFactory, ReverseHttpClientFactory>();

            services
                .AddOptions<HttpMouseOptions>()
                .Bind(this.Configuration.GetSection("HttpMouse"));
        }

        /// <summary>
        /// 配置中间件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="mainConnectionService"></param> 
        public void Configure(IApplicationBuilder app, IHostEnvironment hostEnvironment, IMainConnectionService mainConnectionService)
        {
            app.UseWebSockets();
            app.Use(mainConnectionService.HandleConnectionAsync);

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
