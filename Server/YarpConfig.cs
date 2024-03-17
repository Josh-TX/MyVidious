using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

public class CustomProxyConfigProvider : IProxyConfigProvider
{
    private CustomMemoryConfig _config;

    public CustomProxyConfigProvider(IConfiguration config)
    {
        // Load a basic configuration
        // Should be based on your application needs.
        var routeConfig = new RouteConfig
        {
            RouteId = "route1",
            ClusterId = "cluster1",
            Match = new RouteMatch
            {
                Path = "{username}/{algorithm}/{**catch-all}"
            },
            Transforms = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    {"PathPattern", "{**catch-all}" }
                }
            }
        };

        var routeConfigs = new[] { routeConfig };
        var invidiousUrl = config.GetValue<string>("InvidiousUrl");
        var clusterConfigs = new[]
        {
            new ClusterConfig
            {
                ClusterId = "cluster1",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination1", new DestinationConfig { Address = invidiousUrl! } },
                }
            }
        };

        _config = new CustomMemoryConfig(routeConfigs, clusterConfigs);
    }

    public IProxyConfig GetConfig() => _config;

    /// <summary>
    /// By calling this method from the source we can dynamically adjust the proxy configuration.
    /// Since our provider is registered in DI mechanism it can be injected via constructors anywhere.
    /// </summary>
    public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        var oldConfig = _config;
        _config = new CustomMemoryConfig(routes, clusters);
        oldConfig.SignalChange();
    }

    private class CustomMemoryConfig : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public CustomMemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }

        public IReadOnlyList<ClusterConfig> Clusters { get; }

        public IChangeToken ChangeToken { get; }

        internal void SignalChange()
        {
            _cts.Cancel();
        }
    }
}