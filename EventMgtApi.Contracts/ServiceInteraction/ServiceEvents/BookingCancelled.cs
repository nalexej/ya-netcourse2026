namespace EventMgtApi.Contracts.ServiceInteraction.ServiceEvents;

/// <summary>
/// Контракт события отмены брони.
/// Публикуется издателем (BookingsService) и потребляется подписчиком (EventsService).
/// Содержит данные для освобождения зарезервированных мест.
/// </summary>
public sealed class BookingCancelled
{
    /// <summary>
    /// Идентификатор отменённой брони.
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
    /// Количество освобождаемых мест.
    /// </summary>
    public int SeatsCount { get; }

    /// <summary>
    /// Момент отмены брони (UTC).
    /// </summary>
    public DateTime CancelledAt { get; }

    /// <summary>
    /// Создаёт новый экземпляр события отмены брони.
    /// </summary>
    public BookingCancelled(
        Guid bookingId,
        Guid eventId,
        Guid userId,
        int seatsCount,
        DateTime cancelledAt)
    {
        BookingId = bookingId;
        EventId = eventId;
        UserId = userId;
        SeatsCount = seatsCount;
        CancelledAt = cancelledAt;
    }
}
