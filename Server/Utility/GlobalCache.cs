using Microsoft.Extensions.Caching.Memory;
using System.Security.Policy;

namespace MyVidious.Utilities;

public class GlobalCache
{
    private readonly IMemoryCache _cache;

    public GlobalCache(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public List<int>? GetRandomAlgorithmVideoIds(int algorithmId)
    {
        var key = $"{CacheConstants.RandomAlgorithmVideoIds}:{algorithmId}";
        if (_cache.TryGetValue<List<int>>(key, out var videoIds))
        {
            return videoIds;
        }
        return null;
    }
    public void SetRandomAlgorithmVideoIds(int algorithmId, List<int> videoIds)
    {
        var key = $"{CacheConstants.RandomAlgorithmVideoIds}:{algorithmId}";
        _cache.Set(key, videoIds, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(40)
        });
    }


    public List<int>? GetRecentAlgorithmVideoIds(int algorithmId)
    {
        var key = $"{CacheConstants.RecentAlgorithmVideoIds}:{algorithmId}";
        if (_cache.TryGetValue<List<int>>(key, out var videoIds))
        {
            return videoIds;
        }
        return null;
    }
    public void SetRecentAlgorithmVideoIds(int algorithmId, List<int> videoIds)
    {
        var key = $"{CacheConstants.RecentAlgorithmVideoIds}:{algorithmId}";
        _cache.Set(key, videoIds, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(40)
        });
    }

    public List<string>? GetInvidiousUrls()
    {
        var key = CacheConstants.InvidiousUrls;
        if (_cache.TryGetValue<List<string>>(key, out var urls))
        {
            return urls;
        }
        return null;
    }
    public void SetInvidiousUrls(List<string> urls)
    {
        var key = CacheConstants.InvidiousUrls;
        _cache.Set(key, urls, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        });
    }
}