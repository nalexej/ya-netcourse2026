using System;
using System.Runtime.Serialization;

namespace EventMgtApi.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при превышении лимита активных броней пользователя.
/// </summary>
[Serializable]
public class TooManyActiveBookingsException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TooManyActiveBookingsException"/> с сообщением по умолчанию.
    /// </summary>
    public TooManyActiveBookingsException() : base("Превышен лимит активных бронирований")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TooManyActiveBookingsException"/> с заданным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public TooManyActiveBookingsException(string message) : base(message)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TooManyActiveBookingsException"/>
    /// с заданным сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public TooManyActiveBookingsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
