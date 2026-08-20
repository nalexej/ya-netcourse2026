using EventMgtApi.Contracts.Enums;
using EventMgtApi.UsersService.Domain.Exceptions;

namespace EventMgtApi.UsersService.Domain.Entities;

/// <summary>
/// Представляет сущность пользователя системы.
/// </summary>
public class User
{
    #region Properties

    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Логин пользователя.
    /// </summary>
    public required string Login { get; set; }

    /// <summary>
    /// Хеш пароля пользователя.
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    #endregion

    #region Constructors

    // Приватный конструктор без параметров для ORM
    private User() { }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Создаёт нового пользователя с валидацией входных данных.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="passwordHash">Хеш пароля пользователя.</param>
    /// <param name="role">Роль пользователя (по умолчанию User).</param>
    /// <returns>Созданную сущность User.</returns>
    public static User Create(string login, string passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ValidationException(
                new Dictionary<string, ICollection<string>> { ["Login"] = ["Логин обязателен."] });

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ValidationException(
                new Dictionary<string, ICollection<string>> { ["PasswordHash"] = ["Хеш пароля обязателен."] });

        return new User
        {
            Id = Guid.NewGuid(),
            Login = login.Trim(),
            PasswordHash = passwordHash,
            Role = role
        };
    }

    #endregion
}
