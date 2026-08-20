using EventMgtApi.BookingsService.Domain.Exceptions;
using EventMgtApi.Contracts.Enums;

namespace EventMgtApi.BookingsService.Domain.Entities;

/// <summary>
/// Модель бронирования места на событии.
/// </summary>
public class Booking
{

    #region Properties

    /// <summary>
    /// Уникальный идентификатор брони.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Идентификатор пользователя, создавшего бронь.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Текущий статус брони.
    /// </summary>
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>
    /// Дата и время создания брони.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата и время обработки брони (может быть null).
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    ///// <summary>
    ///// Навигационное свойство: ссылка на связанное событие.
    ///// </summary>
    //public Event? Event { get; private set; } = null!;

    ///// <summary>
    ///// Навигационное свойство: ссылка на пользователя, создавшего бронь.
    ///// </summary>
    //public User? User { get; private set; } = null!;

    #endregion

    #region Constructors

    // Приватный конструктор без параметров для ORM
    private Booking() { }

    /// <summary>
    /// Создаёт новую бронь с начальными значениями: Pending, Id, CreatedAt.
    /// </summary>
    /// <param name="eventId">Идентификатор события, для которого создаётся бронь.</param>
    /// <param name="userId">Идентификатор пользователя, создающего бронь.</param>
    public Booking(Guid eventId, Guid userId)
    {
        EventId = eventId;
        UserId = userId;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Подтверждает бронь, переводя её в статус Confirmed и устанавливая ProcessedAt.
    /// </summary>
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отклоняет бронь, переводя её в статус Rejected и устанавливая ProcessedAt.
    /// </summary>
    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отменяет бронь, переводя её в статус Cancelled.
    /// Бронь можно отменить только если она в статусах Pending либо Confirmed.
    /// Повторная отмена уже отменённой брони вызывает исключение.
    /// </summary>
    /// <exception cref="ValidationException">Если бронь уже имеет статус Cancelled.</exception>
    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new ValidationException(new Dictionary<string, ICollection<string>>
            {
                ["Status"] = ["Бронь уже отменена."]
            });

        if (Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
            throw new ValidationException(new Dictionary<string, ICollection<string>>
            {
                ["Status"] = ["Бронь можно отменить только в статусах Pending или Confirmed."]
            });

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }

    #endregion

}
