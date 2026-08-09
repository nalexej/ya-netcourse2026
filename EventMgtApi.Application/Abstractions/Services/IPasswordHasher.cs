namespace EventMgtApi.Application.Abstractions.Services;

/// <summary>
/// Сервис для хеширования паролей и проверки соответствия.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Хеширует пароль и возвращает строковый хеш.
    /// </summary>
    /// <param name="password">Пароль для хеширования.</param>
    /// <returns>Хеш пароля в виде строки.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Проверяет, соответствует ли пароль хешу.
    /// </summary>
    /// <param name="password">Пароль для проверки.</param>
    /// <param name="hash">Сохранённый хеш.</param>
    /// <returns>True, если пароль совпадает с хешем.</returns>
    bool VerifyPassword(string password, string hash);
}
