using Microsoft.Extensions.Caching.Memory;

namespace MyVidious.Utilities;

public class IPScopedCache
{
    private readonly IMemoryCache _cache;
    private readonly int _ip;

    public IPScopedCache(IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor)
    {
        _cache = memoryCache;
        var ipBytes = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.GetAddressBytes();
        _ip = ipBytes != null && ipBytes.Length >= 4 ? BitConverter.ToInt32(ipBytes) : 0;
    }

    public int? GetAlgorithmPosition(int algorithmId)
    {
        var key = $"{CacheConstants.AlgorithmPosition}:{_ip}:{algorithmId}";
        if (_cache.TryGetValue<int>(key, out int position))
        {
            return position;
        }
        return null;
    }
    public void SetAlgorithmPosition(int algorithmId, int position)
    {
        var key = $"{CacheConstants.AlgorithmPosition}:{_ip}:{algorithmId}";
        _cache.Set<int>(key, position, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(2)
        });
    }
}