using MyVidious.Models.Invidious;
using MyVidious.Utilities;
using Quartz;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;

namespace MyVidious.Access;

public class InvidiousUrlsAccess : IJob
{
    private readonly HttpClient _httpClient;
    private string? _configInvidiousUrl;
    private CustomProxyConfigProvider _customProxyConfigProvider;

    public InvidiousUrlsAccess(AppSettings appSettings, IHttpClientFactory httpClientFactory, CustomProxyConfigProvider customProxyConfigProvider)
    {
        System.Diagnostics.Debug.WriteLine("InvidiousUrlsAccess instantiate: " + DateTime.Now);
        _httpClient = httpClientFactory.CreateClient();
        _configInvidiousUrl = string.IsNullOrEmpty(appSettings.InvidiousUrl) ? null : appSettings.InvidiousUrl;
        _customProxyConfigProvider = customProxyConfigProvider;
        if (_configInvidiousUrl != null)
        {
            _urlPool = new[] { _configInvidiousUrl };
        }
    }
    private static int _index;

    /// <summary>
    /// returns a random invidious url from the pool
    /// </summary>
    public string GetInvidiousUrl()
    {
        _index = (_index + 1) % _urlPool.Count();
        return _urlPool[_index];
    }

    public IList<string> GetAllInvidiousUrls()
    {
        return _urlPool.ToList();
    }

    private async Task<IList<string>> LoadInvidiousInstanceUrls()
    {
        var endpoint = "https://api.invidious.io/instances.json";
        var response = await _httpClient.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Unable to load list of invidious instances from " + endpoint + ". Status code " + response.StatusCode);
            Environment.Exit(1);
        }
        var json = await response.Content.ReadAsStringAsync();
        var instances = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<Instance>>(json);
        var instanceDetails = instances.Select(z => z.Details);
        var invidiousUrls = instanceDetails!.Where(z => z.Api == true && z.Type == "https" && z.Cors == true).Select(z => z.Uri.TrimEnd('/')).ToList();
        return invidiousUrls;
    }

    private IList<string> _urlPool = new List<string>();

    async Task IJob.Execute(IJobExecutionContext context)
    {
        System.Diagnostics.Debug.WriteLine("IJob.Execute: " + DateTime.Now);
        await Task.Delay(2000);
        if (_configInvidiousUrl != null)
        {
            _urlPool = new[] { _configInvidiousUrl };
        } else
        {
            _urlPool = await LoadInvidiousInstanceUrls();
        }

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
        var destinations = new Dictionary<string, DestinationConfig>();
        for (var i = 0; i < _urlPool.Count; i++)
        {
             destinations.Add("d" + i, new DestinationConfig { Address = _urlPool[i]! });
        }
        var clusterConfigs = new[]
        {
            new ClusterConfig
            {
                ClusterId = "cluster1",
                Destinations = destinations,
                LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin
            }
        };
        _customProxyConfigProvider.Update(routeConfigs, clusterConfigs);
    }

    private class InstanceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Instance));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray ja = JArray.Load(reader);
            Instance stop = new Instance();
            stop.Name = (string)ja[0]!;
            stop.Details = ja[1].ToObject<InstancesResponseDetails>()!;
            return stop;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    [JsonConverter(typeof(InstanceConverter))]
    private class Instance
    {
        public string Name { get; set; }
        public InstancesResponseDetails Details { get; set; }
    }

    private class InstancesResponseDetails
    {
        public bool? Api { get; set; }
        public bool? Cors { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
    }
}