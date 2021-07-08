using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Yarp.ReverseProxy.Configuration;

namespace HttpMouse.Configuration
{
    sealed class InMemoryConfigProvider : IProxyConfigProvider
    {
        private volatile InMemoryConfig config = new();

        private readonly ConcurrentDictionary<string, string> datas = new();

        public IProxyConfig GetConfig()
        {
            return this.config;
        }

        public bool Add(string clientDomain, string clinetUpstream)
        {
            var value = this.datas.GetOrAdd(clientDomain, clinetUpstream);
            var changed = value == clinetUpstream;

            if (changed == true)
            {
                this.UpdateConfig();
            }

            return changed;
        }

        public void Remove(string clientDoamin)
        {
            if (this.datas.TryRemove(clientDoamin, out _))
            {
                this.UpdateConfig();
            }
        }

        private void UpdateConfig()
        {
            var oldConfig = config;

            var keyValues = this.datas.ToArray();
            var routes = keyValues.Select(item => GetRouteConfig(item.Key)).ToArray();
            var clusters = keyValues.Select(item => GetClusterConfig(item.Key, item.Value)).ToArray();
            config = new InMemoryConfig(routes, clusters);
            oldConfig.SignalChange();
        }

        private static ClusterConfig GetClusterConfig(string clientDomain, string clientUpstream)
        {
            var destinations = new Dictionary<string, DestinationConfig>
            {
                [clientUpstream] = new DestinationConfig { Address = clientUpstream }
            };

            return new ClusterConfig
            {
                ClusterId = clientDomain,
                Destinations = destinations
            };
        }

        private static RouteConfig GetRouteConfig(string clientDomain)
        {
            return new RouteConfig
            {
                RouteId = clientDomain,
                ClusterId = clientDomain,
                Match = new RouteMatch
                {
                    Hosts = new List<string> { clientDomain }
                }
            };
        }


        private class InMemoryConfig : IProxyConfig
        {
            private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();

            public IReadOnlyList<RouteConfig> Routes { get; }

            public IReadOnlyList<ClusterConfig> Clusters { get; }

            public IChangeToken ChangeToken { get; }

            public InMemoryConfig()
                : this(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>())
            {
            }

            public InMemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
            {
                this.Routes = routes;
                this.Clusters = clusters;
                this.ChangeToken = new CancellationChangeToken(cancellationToken.Token);
            }

            public void SignalChange()
            {
                this.cancellationToken.Cancel();
            }
        }
    }
}
