namespace EventMgtApi.Contracts.ServiceInteraction.ServiceEvents;

/// <summary>
/// Контракт события отмены подтверждения брони (не хватило мест или событие началось).
/// Публикуется издателем (EventsService) и потребляется подписчиком (BookingsService).
/// </summary>
public sealed class BookingConfirmationFailed
{
    /// <summary>
    /// Идентификатор брони, которую нужно отклонить.
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
    /// Создаёт новый экземпляр события отмены подтверждения.
    /// </summary>
    public BookingConfirmationFailed(
        Guid bookingId,
        Guid eventId,
        Guid userId)
    {
        BookingId = bookingId;
        EventId = eventId;
        UserId = userId;
    }
}
