using EventMgtApi.UsersService.Application.Abstractions.Services;
using Microsoft.AspNetCore.Identity;

namespace EventMgtApi.UsersService.Infrastructure.Services;

/// <summary>
/// Реализация IPasswordHasher, использующая встроенный в ASP.NET Core PasswordHasher.
/// Использует PBKDF2 с солью, что безопасно против радужных таблиц.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string HashPassword(string password)
    {
        // Передаем null как "user", так как для хэширования пароля данные пользователя не нужны
        return _passwordHasher.HashPassword(null!, password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        // Передаем null как "user"
        var result = _passwordHasher.VerifyHashedPassword(null!, hash, password);
        return result == PasswordVerificationResult.Success;
    }
}