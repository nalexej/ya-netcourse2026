using EventMgtApi.Application.Users.DTOs;

namespace EventMgtApi.Application.Abstractions.Services;

/// <summary>
/// Интерфейс сервиса аутентификации и регистрации пользователей.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Регистрирует нового пользователя с хешированием пароля.
    /// </summary>
    /// <param name="request">Данные регистрации.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO зарегистрированного пользователя.</returns>
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет вход пользователя: проверяет логин и пароль, возвращает JWT-токен.
    /// </summary>
    /// <param name="request">Данные для входа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO с токеном и данными пользователя.</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
