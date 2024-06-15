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
    public static string INSTANCES_URL = "https://api.invidious.io/instances.json";
    /// <summary>
    /// This is a fake baseURL used for storing urls to the database. We should always use this fake baseURL when storing to the database, since we don't want stored urls tied to a specific instance. 
    /// </summary>
    public static string STORAGE_URL = "http://invidious.com";

    private IList<string> _internalPool = new List<string>();
    private IList<string> _externalPool = new List<string>();
    private readonly HttpClient _httpClient;
    private bool _internalUsesPool;
    private bool _externalUsesPool;
    //private string _internalInvidiousUrl;
    //private string _externalInvidiousUrl;
    private CustomProxyConfigProvider _customProxyConfigProvider;

    public InvidiousUrlsAccess(AppSettings appSettings, IHttpClientFactory httpClientFactory, CustomProxyConfigProvider customProxyConfigProvider)
    {
        System.Diagnostics.Debug.WriteLine("InvidiousUrlsAccess instantiate: " + DateTime.Now);
        _httpClient = httpClientFactory.CreateClient();
        _customProxyConfigProvider = customProxyConfigProvider;
        _internalUsesPool = appSettings.InternalInvidiousUrl!.ToLower() == "pool";
        _externalUsesPool = appSettings.ExternalInvidiousUrl!.ToLower() == "pool";
        //_externalInvidiousUrl = appSettings.ExternalInvidiousUrl!;
        if (!_internalUsesPool)
        {
            _internalPool = new[] { appSettings.InternalInvidiousUrl! };
        }
        if (!_externalUsesPool && !string.IsNullOrEmpty(appSettings.ExternalInvidiousUrl))
        {
            _externalPool = new[] { appSettings.ExternalInvidiousUrl };
        }
    }
    private static int _index;

    /// <summary>
    /// returns a random internal invidious url from the pool
    /// </summary>
    public string GetInternalInvidiousUrl()
    {
        _index = (_index + 1) % _internalPool.Count();
        return _internalPool[_index];
    }

    /// <summary>
    /// returns a random external invidious url from the pool
    /// </summary>
    public string GetExternalInvidiousUrl()
    {
        _index = (_index + 1) % _externalPool.Count();
        return _externalPool[_index];
    }

    public string GetUrlForStorage(string inputUrl)
    {
        var match = _internalPool.FirstOrDefault(url => inputUrl.StartsWith(url));
        if (match != null)
        {
            return STORAGE_URL + inputUrl.Substring(match.Length);
        }
        return inputUrl;
    }

    public IEnumerable<string> GetInternalUrls()
    {
        return _internalPool;
    }

    private async Task<IList<string>> LoadInvidiousInstanceUrls()
    {
        var response = await _httpClient.GetAsync(INSTANCES_URL);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Unable to load list of invidious instances from " + INSTANCES_URL + ". Status code " + response.StatusCode);
            Environment.Exit(1);
        }
        var json = await response.Content.ReadAsStringAsync();
        var instances = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<Instance>>(json);
        var instanceDetails = instances!.Select(z => z.Details);
        var invidiousUrls = instanceDetails!.Where(z => z?.Api == true && z.Type == "https" && z.Cors == true).Select(z => z!.Uri.TrimEnd('/')).ToList();
        return invidiousUrls;
    }


    async Task IJob.Execute(IJobExecutionContext context)
    {
        System.Diagnostics.Debug.WriteLine("IJob.Execute: " + DateTime.Now);
        await Task.Delay(2000);

        if (_internalUsesPool || _externalUsesPool)
        {
            var urlPool = await LoadInvidiousInstanceUrls();
            if (_internalUsesPool)
            {
                _internalPool = urlPool;
            }
            if (_externalUsesPool)
            {
                _externalPool = urlPool;
            }
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
        for (var i = 0; i < _internalPool.Count; i++)
        {
             destinations.Add("d" + i, new DestinationConfig { Address = _internalPool[i]! });
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

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JArray ja = JArray.Load(reader);
            Instance stop = new Instance();
            stop.Name = (string)ja[0]!;
            stop.Details = ja[1].ToObject<InstancesResponseDetails>()!;
            return stop;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    [JsonConverter(typeof(InstanceConverter))]
    private class Instance
    {
        public string? Name { get; set; }
        public InstancesResponseDetails? Details { get; set; }
    }

    private class InstancesResponseDetails
    {
        public bool? Api { get; set; }
        public bool? Cors { get; set; }
        public string? Type { get; set; }
        public required string Uri { get; set; }
    }
}