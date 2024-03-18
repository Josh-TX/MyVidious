using MyVidious.Models.Invidious;
using MyVidious.Utilities;

namespace MyVidious.Access;

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
    private static int _index;

    public async Task<string> GetInvidiousUrl()
    {
        var urls = await GetInvidiousUrls();
        _index = (_index + 1) % urls.Count();
        return urls[_index];
    }

    public Task<IList<string>> GetAllInvidiousUrls()
    {
        return GetInvidiousUrls();
    }

    private async Task<IList<string>> GetInvidiousUrls()
    {
        return _configInvidiousUrl != null
            ? new[] { _configInvidiousUrl }
            : await GetInvidiousInstanceUrls();
    }

    private async Task<IList<string>> GetInvidiousInstanceUrls()
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
        var invidiousUrls = instances!.Where(z => z.Item2.Api && z.Item2.Type == "https" && z.Item2.Cors).Select(z => z.Item2.Uri.TrimEnd('/')).ToList();
        _globalCache.SetInvidiousUrls(invidiousUrls);
        return invidiousUrls;
    }

    private class InstancesResponseDetails
    {
        public bool Api { get; set; }
        public bool Cors { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
    }
}