namespace EventMgtApi.Contracts.Users.DTOs;

/// <summary>
/// Запрос на вход пользователя в систему.
/// </summary>
public sealed class LoginRequestDto
{
    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// Пароль пользователя.
    /// </summary>
    public required string Password { get; init; }
}

/// <summary>
/// Запрос на регистрацию нового пользователя.
/// </summary>
public sealed class RegisterRequestDto
{
    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// Пароль пользователя.
    /// </summary>
    public required string Password { get; init; }
}

/// <summary>
/// Ответ после успешной авторизации: токен и данные пользователя.
/// </summary>
public sealed class LoginResponseDto
{
    /// <summary>
    /// JWT-токен.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public required string Role { get; init; }
}

/// <summary>
/// Ответ после успешной регистрации.
/// </summary>
public sealed class RegisterResponseDto
{
    /// <summary>
    /// ID зарегистрированного пользователя.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public required string Role { get; init; }
}
