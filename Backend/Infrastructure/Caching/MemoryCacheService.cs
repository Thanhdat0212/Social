using System.Collections.Concurrent;
using Application.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Caching;

/// <summary>
/// Cài đặt ICacheService sử dụng IMemoryCache nội bộ RAM của ASP.NET Core
/// Đóng vai trò vừa là bộ nhớ đệm nhẹ nội bộ, vừa là phương án dự phòng (Fallback) khi không kết nối Redis
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _activeKeys = new();

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("MemoryCache HIT: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("MemoryCache MISS: {Key}", key);
        return Task.FromResult(default(T));
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("MemoryCache HIT: {Key}", key);
            return cachedValue;
        }

        _logger.LogDebug("MemoryCache MISS: {Key} -> Đang nạp dữ liệu từ Database...", key);
        var freshValue = await factory(cancellationToken);

        if (freshValue != null)
        {
            Set(key, freshValue, expiration);
        }

        return freshValue;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        Set(key, value, expiration);
        return Task.CompletedTask;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(20)
        };

        options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            if (evictedKey is string strKey)
            {
                _activeKeys.TryRemove(strKey, out _);
            }
        });

        _memoryCache.Set(key, value, options);
        _activeKeys.TryAdd(key, 0);
        _logger.LogDebug("Đã lưu vào Memory Cache: {Key} (Hết hạn: {Expiration})", key, options.AbsoluteExpirationRelativeToNow);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
        _activeKeys.TryRemove(key, out _);
        _logger.LogInformation("MemoryCache INVALIDATED: {Key}", key);
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        RemoveByPrefix(prefix);
        return Task.CompletedTask;
    }

    public void RemoveByPrefix(string prefix)
    {
        var keysToRemove = _activeKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _activeKeys.TryRemove(key, out _);
        }

        _logger.LogInformation("MemoryCache INVALIDATED prefix '{Prefix}': Đã xóa {Count} keys", prefix, keysToRemove.Count);
    }
}
