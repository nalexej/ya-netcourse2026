using EventMgtApi.Domain.Entities;

namespace EventMgtApi.Application.Abstractions.Services;

/// <summary>
/// Сервис для начального заполнения базы данных (seed).
/// </summary>
public interface ISeedService
{
    /// <summary>
    /// Создаёт начальных пользователей, если они ещё не существуют.
    /// </summary>
    Task SeedAsync();
}
