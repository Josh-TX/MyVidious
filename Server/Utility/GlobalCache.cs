using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Security.Policy;

namespace MyVidious.Utilities;

public class GlobalCache
{
    private readonly IMemoryCache _cache;
    private static readonly ConcurrentDictionary<int, DateTime> algorithmLastUpdate = new ConcurrentDictionary<int, DateTime>();
    private const int UPDATE_MINUTES = 3;

    public GlobalCache(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public void HandleAlgorithmUpdated(int algorithmId)
    {
        _cache.Remove($"{CacheConstants.RandomAlgorithmVideoIds}:{algorithmId}");
        _cache.Remove($"{CacheConstants.RecentAlgorithmVideoIds}:{algorithmId}");
        algorithmLastUpdate.TryAdd(algorithmId, DateTime.UtcNow);
        foreach (var pair in algorithmLastUpdate)
        {
            if ((DateTime.UtcNow - pair.Value).TotalMinutes > UPDATE_MINUTES)
            {
                algorithmLastUpdate.TryRemove(pair);
            }
        }
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
        var time = TimeSpan.FromMinutes(30);
        if (algorithmLastUpdate.TryGetValue(algorithmId, out var lastUpdate) && (DateTime.UtcNow - lastUpdate).TotalMinutes < UPDATE_MINUTES)
        {
            time = TimeSpan.FromMinutes(1);
        }
        _cache.Set(key, videoIds, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = time
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
        var time = TimeSpan.FromMinutes(30);
        if (algorithmLastUpdate.TryGetValue(algorithmId, out var lastUpdate) && (DateTime.UtcNow - lastUpdate).TotalMinutes < UPDATE_MINUTES)
        {
            time = TimeSpan.FromMinutes(1);
        }
        _cache.Set(key, videoIds, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = time
        });
    }
}