using System;
using System.Runtime.Serialization;

namespace EventMgtApi.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при отсутствии прав у пользователя на выполнение операции.
/// </summary>
[Serializable]
public class ForbiddenException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ForbiddenException"/> с сообщением по умолчанию.
    /// </summary>
    public ForbiddenException() : base("Недостаточно прав для выполнения операции")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ForbiddenException"/> с заданным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public ForbiddenException(string message) : base(message)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ForbiddenException"/>
    /// с заданным сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
