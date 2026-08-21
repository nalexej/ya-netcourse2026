namespace EventMgtApi.Contracts.ServiceInteraction.ServiceEvents;

/// <summary>
/// Контракт события подтверждения брони.
/// Публикуется издателем (BookingsService) и потребляется подписчиком (EventsService).
/// Содержит только данные, необходимые подписчику для дальнейшей обработки.
/// </summary>
public sealed class BookingConfirmed
{
    /// <summary>
    /// Идентификатор подтверждённой брони.
    /// </summary>
    public Guid BookingId { get; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Идентификатор пользователя, создавшего бронь.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Количество забронированных мест.
    /// </summary>
    public int SeatsCount { get; }

    /// <summary>
    /// Момент подтверждения брони (UTC).
    /// </summary>
    public DateTime ConfirmedAt { get; }

    /// <summary>
    /// Создаёт новый экземпляр события подтверждения брони.
    /// </summary>
    public BookingConfirmed(
        Guid bookingId,
        Guid eventId,
        Guid userId,
        int seatsCount,
        DateTime confirmedAt)
    {
        BookingId = bookingId;
        EventId = eventId;
        UserId = userId;
        SeatsCount = seatsCount;
        ConfirmedAt = confirmedAt;
    }
}
