using EventMgtApi.Domain.Entities;

namespace EventMgtApi.Application.Abstractions.Services;

/// <summary>
/// Сервис для генерации и валидации JWT-токенов.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Генерирует JWT-токен для указанного пользователя.
    /// </summary>
    /// <param name="user">Пользователь, для которого генерируется токен.</param>
    /// <returns>Строка токена.</returns>
    string GenerateToken(User user);
}
