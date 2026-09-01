namespace EventMgtApi.EventsService.Application.Persistence;

/// <summary>
/// Интерфейс кэша — изолирует Application-слой от конкретной реализации (Redis и т.д.).
/// </summary>
public interface ICacheClient
{
    /// <summary>
    /// Получить строковое значение по ключу.
    /// </summary>
    Task<string?> GetStringAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Записать строковое значение с временем жизни (TTL в секундах; 0 — без TTL).
    /// </summary>
    Task SetStringAsync(string key, string value, TimeSpan? expiresIn = null, CancellationToken ct = default);

    /// <summary>
    /// Удалить ключ из кэша.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);
}
