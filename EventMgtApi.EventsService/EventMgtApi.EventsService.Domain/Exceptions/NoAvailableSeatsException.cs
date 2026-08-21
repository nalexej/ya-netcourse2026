
namespace EventMgtApi.EventsService.Domain.Exceptions;

/// <summary>
/// Исключение, выбрасываемое при отсутствии доступных мест на событие.
/// </summary>
[Serializable]
public class NoAvailableSeatsException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="NoAvailableSeatsException"/> 
    /// с сообщением по умолчанию
    /// </summary>
    public NoAvailableSeatsException() : base("No available seats for this event")
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса  <see cref="NoAvailableSeatsException"/> с заданным сообщением об ошибке.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public NoAvailableSeatsException(string message) : base(message)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="NoAvailableSeatsException"/> 
    /// с заданным сообщением и внутренним исключением, которое стало причиной этого исключения.
    /// </summary>
    /// <param name="message">Сообщение об ошибке, объясняющее причину исключения.</param>
    /// <param name="innerException">Внутреннее исключение, вызвавшее данное исключение.</param>
    public NoAvailableSeatsException(string message, Exception innerException) : base(message, innerException)
    {
    }

}
