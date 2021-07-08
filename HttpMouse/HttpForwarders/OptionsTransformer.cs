using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace HttpMouse.HttpForwarders
{
    sealed class OptionsTransformer : HttpTransformer
    {
        private static readonly HttpRequestOptionsKey<string> clientDomainKey = new("ClientDomain");

        new public static readonly OptionsTransformer Default = new(); 

        public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
        {
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);
            proxyRequest.Headers.Host = null;
            proxyRequest.Options.Set(clientDomainKey, httpContext.Request.Host.Host);
        }
    }
}
