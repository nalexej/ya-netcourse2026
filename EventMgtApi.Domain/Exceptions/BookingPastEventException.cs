using System;
using System.Runtime.Serialization;

namespace EventMgtApi.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при попытке отменить бронирование прошедшего события.
/// </summary>
[Serializable]
public class BookingPastEventException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingPastEventException"/> с сообщением по умолчанию.
    /// </summary>
    public BookingPastEventException() : base("Нельзя отменить бронь на прошедшее событие")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingPastEventException"/> с заданным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public BookingPastEventException(string message) : base(message)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BookingPastEventException"/>
    /// с заданным сообщением и внутренним исключением.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public BookingPastEventException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
