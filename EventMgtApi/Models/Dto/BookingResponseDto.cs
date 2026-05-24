using System;
using System.Text.Json.Serialization;

namespace EventMgtApi.Models;

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