using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis; // trực tiếp giao tiếp với redis qua server của https://redis.io/cloud/

namespace Infrastructure.External.Caching;

/// <summary>
/// Cài đặt ICacheService sử dụng Redis (StackExchange.Redis) cho Distributed Cache
/// Tích hợp:
/// 1. Serialization JSON tối ưu qua System.Text.Json
/// 2. Fail-safe Fallback: Nếu Redis mất kết nối, hệ thống tự động gọi Database mà KHÔNG bị lỗi 500
/// 3. Xóa theo prefix (RemoveByPrefixAsync) phục vụ Cache Invalidation thông minh
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _instanceName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
        _database = _redis.GetDatabase();

        // Tiền tố cho key, ví dụ "social:" để gom nhóm gọn gàng trên Redis Insight
        _instanceName = configuration["Redis:InstanceName"] ?? "social:";
    }

    private string GetFullKey(string key)
    {
        return key.StartsWith(_instanceName) ? key : $"{_instanceName}{key}";
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis chưa sẵn sàng kết nối. Tự động fallback bỏ qua đọc cache cho key {Key}.", key);
            return default;
        }

        var fullKey = GetFullKey(key);
        try
        {
            var redisValue = await _database.StringGetAsync(fullKey);
            if (!redisValue.HasValue)
            {
                _logger.LogDebug("Redis MISS: {Key}", fullKey);
                return default;
            }

            _logger.LogDebug("Redis HIT: {Key}", fullKey);
            return JsonSerializer.Deserialize<T>(redisValue.ToString(), JsonOptions);
        }
        catch (Exception ex)
        {
            // Fail-safe: Nếu Redis có sự cố mạng, log warning và coi như cache miss để hệ thống không sập
            _logger.LogWarning(ex, "Lỗi khi đọc Redis key {Key}. Đang fallback sang nguồn gốc.", fullKey);
            return default;
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Thử đọc từ Redis trước
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // 2. Cache MISS -> Chạy factory lấy dữ liệu tươi từ Database
        _logger.LogDebug("Redis MISS: {Key} -> Đang truy vấn dữ liệu từ Database...", key);
        var freshValue = await factory(cancellationToken);

        // 3. Nếu lấy được dữ liệu, ghi lại vào Redis cho các request sau
        if (freshValue != null)
        {
            await SetAsync(key, freshValue, expiration, cancellationToken);
        }

        return freshValue;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (value == null) return;

        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis chưa sẵn sàng kết nối. Bỏ qua ghi cache key {Key}.", key);
            return;
        }

        var fullKey = GetFullKey(key);
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var ttl = expiration ?? TimeSpan.FromHours(1);

            await _database.StringSetAsync(fullKey, json, ttl);
            _logger.LogDebug("Redis SET: {Key} (TTL: {Ttl})", fullKey, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi dữ liệu vào Redis key {Key}.", fullKey);
        }
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        // Wrapper đồng bộ (Fire-and-forget an toàn)
        Task.Run(() => SetAsync(key, value, expiration));
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected) return;

        var fullKey = GetFullKey(key);
        try
        {
            var removed = await _database.KeyDeleteAsync(fullKey);
            if (removed)
            {
                _logger.LogInformation("Redis INVALIDATED: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa Redis key {Key}.", fullKey);
        }
    }

    public void Remove(string key)
    {
        Task.Run(() => RemoveAsync(key));
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var searchPattern = $"{GetFullKey(prefix)}*";
        try
        {
            var endpoints = _redis.GetEndPoints();
            var totalDeleted = 0L;

            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsConnected) continue;

                var keys = server.Keys(pattern: searchPattern).ToArray();
                if (keys.Length > 0)
                {
                    var deleted = await _database.KeyDeleteAsync(keys);
                    totalDeleted += deleted;
                }
            }

            _logger.LogInformation("Redis INVALIDATED prefix '{Pattern}': Đã xóa {Count} keys", searchPattern, totalDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa Redis theo tiền tố {Pattern}.", searchPattern);
        }
    }

    public void RemoveByPrefix(string prefix)
    {
        Task.Run(() => RemoveByPrefixAsync(prefix));
    }
}
