using EventMgtApi.Domain.Enums;
using System;

namespace EventMgtApi.Domain.Entities;

/// <summary>
/// Модель бронирования места на событии.
/// </summary>
public class Booking
{
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
    /// Создаёт новую бронь с начальными значениями: Pending, Id, CreatedAt.
    /// </summary>
    /// <param name="eventId">Идентификатор события, для которого создаётся бронь.</param>
    public Booking(Guid eventId)
    {
        EventId = eventId;
    }
}