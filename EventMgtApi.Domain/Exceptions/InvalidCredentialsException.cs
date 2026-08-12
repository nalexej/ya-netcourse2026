
namespace EventMgtApi.Domain.Exceptions;

/// <summary>
/// Ошибка авторизации.
/// </summary>
[Serializable]
public class InvalidCredentialsException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="InvalidCredentialsException"/> с сообщением по умолчанию.
    /// </summary>
    public InvalidCredentialsException() : base("Неверный логин или пароль.")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="InvalidCredentialsException"/> с заданным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public InvalidCredentialsException(string message) : base(message)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="InvalidCredentialsException"/>
    /// с заданным сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public InvalidCredentialsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
