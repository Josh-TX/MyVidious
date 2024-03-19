using MyVidious.Models;
using MyVidious.Models.Invidious;

namespace MyVidious.Access;

public class InvidiousAPIAccess
{
    private readonly HttpClient _httpClient;
    private readonly InvidiousUrlsAccess _invidiousUrlsAccess;

    public InvidiousAPIAccess(IHttpClientFactory httpClientFactory, AppSettings appSettings, InvidiousUrlsAccess invidiousUrlsAccess)
    {
        _httpClient = httpClientFactory.CreateClient();
        _invidiousUrlsAccess = invidiousUrlsAccess;
    }

    public async Task<VideoResponse> GetVideo(string videoId)
    {
        var url = _invidiousUrlsAccess.GetInvidiousUrl() + "/api/v1/videos/" + videoId;
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Invidious API returned status code " + (int)response.StatusCode);
        }
        var json = await response.Content.ReadAsStringAsync();
        var data = System.Text.Json.JsonSerializer.Deserialize<VideoResponse>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true});
        return data;
    }

    public async Task<IEnumerable<SearchResponseBase>> Search(SearchRequest request)
    {
        var queryDict = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(request.Q))
        {
            queryDict.Add("q", request.Q);
        }
        if (request.Page.HasValue)
        {
            queryDict.Add("sage", request.Page.Value.ToString());
        }
        if (!string.IsNullOrEmpty(request.Sort_By))
        {
            queryDict.Add("sort_By", request.Sort_By);
        }
        if (!string.IsNullOrEmpty(request.Date))
        {
            queryDict.Add("date", request.Date);
        }
        if (!string.IsNullOrEmpty(request.Duration))
        {
            queryDict.Add("duration", request.Duration);
        }
        if (!string.IsNullOrEmpty(request.Type))
        {
            queryDict.Add("type", request.Type);
        }
        if (!string.IsNullOrEmpty(request.Features))
        {
            queryDict.Add("features", request.Features);
        }
        if (!string.IsNullOrEmpty(request.Region))
        {
            queryDict.Add("region", request.Region);
        }
        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("", queryDict);
        var url = _invidiousUrlsAccess.GetInvidiousUrl() + "/api/v1/search" + queryParams;
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Invidious API returned status code " + (int)response.StatusCode);
        }
        var json = await response.Content.ReadAsStringAsync();
        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<SearchResponseBase>>(json, new Newtonsoft.Json.JsonSerializerSettings
        {
            Converters = new[] { new SearchResponseConverter() }
        });
        return data!;
    }

    public async Task<ChannelResponse> GetChannel(string channelId)
    {
        var url = _invidiousUrlsAccess.GetInvidiousUrl() + "/api/v1/channels/" + channelId;
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Invidious API returned status code " + (int)response.StatusCode);
        }
        var json = await response.Content.ReadAsStringAsync();
        var data = System.Text.Json.JsonSerializer.Deserialize<ChannelResponse>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return data!;
    }

    public async Task<ChannelVideosResponse> GetChannelVideos(string channelId, ChannelVideosRequest request)
    {
        var queryDict = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(request.Sort_by))
        {
            queryDict.Add("sort_by", request.Sort_by);
        }
        if (!string.IsNullOrEmpty(request.Continuation))
        {
            queryDict.Add("continuation", request.Continuation);
        }
        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("", queryDict);
        var url = _invidiousUrlsAccess.GetInvidiousUrl() + "/api/v1/channels/" + channelId + "/videos" + queryParams;
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Invidious API returned status code " + (int)response.StatusCode);
        }
        var json = await response.Content.ReadAsStringAsync();
        var data = System.Text.Json.JsonSerializer.Deserialize<ChannelVideosResponse>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return data!;
    }
}
