using EventMgtApi.EventsService.Application.Caching;
using EventMgtApi.Contracts.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventMgtApi.EventsService.Infrastructure.Persistence;

/// <summary>
/// Redis-реализация кэша. При недоступности Redis логировать ошибку и
/// возвращать null / молча игнорировать — кеш деградирует, клиент идёт в БД.
/// </summary>
public sealed class RedisCacheClient : ICacheClient
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheClient> _logger;

    public RedisCacheClient(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheClient> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis is not connected. Cache miss for key '{Key}'.", key);
            return null;
        }

        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GetStringAsync failed for key '{Key}'. Falling back to DB.", key);
            return null;
        }
    }

    public async Task SetStringAsync(
        string key,
        string value,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis is not connected. Skipping cache set for key '{Key}'.", key);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var expiration = expiresIn.HasValue ? (Expiration)expiresIn.Value : default;
            await db.StringSetAsync(key, value, expiration, flags: CommandFlags.FireAndForget);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SetStringAsync failed for key '{Key}'.", key);
            // Кеш не обновлён — это не критично, клиент продолжит работать с БД.
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis is not connected. Skipping cache remove for key '{Key}'.", key);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key, flags: CommandFlags.FireAndForget);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis RemoveAsync failed for key '{Key}'.", key);
        }
    }
}
