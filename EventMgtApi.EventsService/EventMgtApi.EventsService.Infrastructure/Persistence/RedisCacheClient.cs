using EventMgtApi.Contracts.Options;
using EventMgtApi.EventsService.Application.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventMgtApi.EventsService.Infrastructure.Persistence;

/// <summary>
/// Redis-реализация кэша. При недоступности Redis логировать ошибку и
/// возвращать null / молча игнорировать — кеш деградирует, клиент идёт в БД.
/// </summary>
public sealed class RedisCacheClient : ICacheClient, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheClient> _logger;
    private readonly bool _isConnected;

    public RedisCacheClient(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheClient> logger)
    {
        _redis = redis;
        _logger = logger;
        _isConnected = redis.IsConnected;

        // Подписываемся на событие потери соединения
        redis.ConnectionRestored += (_, _) =>
            _logger.LogInformation("Redis connection restored.");

        redis.ConnectionFailed += (_, _) =>
            _logger.LogWarning("Redis connection lost.");
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        if (!_isConnected)
        {
            _logger.LogWarning("Redis is not connected. Cache miss for key '{Key}'.", key);
            return null;
        }

        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key, CommandFlags.FireAndForget);
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
        if (!_isConnected)
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
        if (!_isConnected)
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

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
