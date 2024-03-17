using Microsoft.Extensions.Caching.Memory;

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
}