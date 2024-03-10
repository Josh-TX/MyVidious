using Microsoft.Extensions.Caching.Memory;

namespace MyVidious.Utilities;

public class GlobalCache
{
    private readonly IMemoryCache _cache;

    public GlobalCache(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public List<int>? GetAlgorithmVideoIds(int algorithmId)
    {
        var key = $"{CacheConstants.AlgorithmVideoIds}:{algorithmId}";
        if (_cache.TryGetValue<List<int>>(key, out var videoIds))
        {
            return videoIds;
        }
        return null;
    }
    public void SetAlgorithmPosition(int algorithmId, List<int> videoIds)
    {
        var key = $"{CacheConstants.AlgorithmVideoIds}:{algorithmId}";
        _cache.Set(key, videoIds, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(40)
        });
    }
}