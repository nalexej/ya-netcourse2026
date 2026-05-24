using System;

namespace EventMgtApi.Models;

/// <summary>
/// Модель бронирования места на событии.
/// </summary>
public class Booking
{
    /// <summary>
    /// Уникальный идентификатор брони.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь.
    /// </summary>
    public required Guid EventId { get; set; }

    /// <summary>
    /// Текущий статус брони.
    /// </summary>
    public required BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания брони.
    /// </summary>
    public required DateTime CreatedAt { get; set; }

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
        Id = Guid.NewGuid();
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Приватный конструктор для ORM/сериализаторов.
    /// </summary>
    private Booking() { }
}