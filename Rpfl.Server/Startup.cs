using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Yarp.ReverseProxy.Abstractions.Config;

namespace Rpfl.Server
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpProxy();
            services.AddServiceAndOptions(this.GetType().Assembly, this.Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ProxyService proxyService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();
            app.Use(next => async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var service = context.RequestServices.GetRequiredService<MainConnectionService>();
                    await service.OnConnectedAsync(context);
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
                endpoints.Map("/{**catch-all}", proxyService.ProxyAsync);
            });
        }
    }
}
