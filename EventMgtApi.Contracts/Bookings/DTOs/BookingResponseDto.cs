using EventMgtApi.Contracts.Enums;

namespace EventMgtApi.Contracts.Bookings.DTOs;

/// <summary>
/// DTO для возврата информации о брони.
/// </summary>
public class BookingResponseDto
{
    /// <summary>
    /// Уникальный идентификатор брони.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор события.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Идентификатор пользователя, создавшего бронь.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Текущий статус брони.
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания брони.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки брони (может быть null).
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
