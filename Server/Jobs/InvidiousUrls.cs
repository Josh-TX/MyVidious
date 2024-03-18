
using Azure;
using MyVidious.Models.Invidious;
using MyVidious.Utilities;
using Quartz;


public class InvidiousInstancesAccess
{
    private readonly HttpClient _httpClient;
    private string? _configInvidiousUrl;
    private GlobalCache _globalCache;

    public InvidiousInstancesAccess(AppSettings appSettings, IHttpClientFactory httpClientFactory, GlobalCache globalCache)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configInvidiousUrl = string.IsNullOrEmpty(appSettings.InvidiousUrl) ? null : appSettings.InvidiousUrl;
        _globalCache = globalCache;
    }

    private async Task<IEnumerable<string>> getInvidiousUrls()
    {
        return _configInvidiousUrl != null
            ? new[] { _configInvidiousUrl }
            : await GetInvidiousInstanceUrls();
    }

    private async Task<IEnumerable<string>> GetInvidiousInstanceUrls()
    {
        var cachedUrls = _globalCache.GetInvidiousUrls();
        if (cachedUrls != null)
        {
            return cachedUrls;
        }
        var endpoint = "https://api.invidious.io/instances.json";
        var response = await _httpClient.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Unable to load list of invidious instances from " + endpoint + ". Status code " + response.StatusCode);
            Environment.Exit(1);
        }
        var json = await response.Content.ReadAsStringAsync();
        var instances = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<(string, InstancesResponseDetails)>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var invidiousUrls = instances!.Where(z => z.Item2.api && z.Item2.type == "https" && z.Item2.cors).Select(z => z.Item2.uri.TrimEnd('/')).ToList();
        _globalCache.SetInvidiousUrls(invidiousUrls);
        return invidiousUrls;
    }

    private class InstancesResponseDetails
    {
        public bool api { get; set; }
        public bool cors { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }
}

public class InvidiousInstanceAccess : IJob
{
    private readonly ILogger<QuartzJob> _logger;
    public InvidiousInstanceAccess(ILogger<QuartzJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation(DateTime.UtcNow.ToString());
        return Task.CompletedTask;
    }
}