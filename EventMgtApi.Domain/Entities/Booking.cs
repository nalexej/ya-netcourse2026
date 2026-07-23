using EventMgtApi.Domain.Enums;
using System;

namespace EventMgtApi.Domain.Entities;

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

    /// <summary>
    /// Навигационное свойство: ссылка на связанное событие 
    /// </summary>
    public Event? Event { get; private set; } = null!;

    #endregion

    #region Constructors

    // Приватный конструктор без параметров для ORM 
    private Booking() { }

    /// <summary>
    /// Создаёт новую бронь с начальными значениями: Pending, Id, CreatedAt.
    /// </summary>
    /// <param name="eventId">Идентификатор события, для которого создаётся бронь.</param>
    public Booking(Guid eventId)
    {
        EventId = eventId;
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

    #endregion

}