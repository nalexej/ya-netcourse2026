
namespace EventMgtApi.EventsService.Domain.Entities;

/// <summary>
/// Запись об обработанном бронировании — для идемпотентности.
/// Хранит пару (EventId, BookingId), чтобы не обрабатывать одно и то же дважды.
/// </summary>
public class ProcessedBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid BookingId { get; set; }
    public string EventType { get; set; } = string.Empty; // "Confirmed", "Cancelled" или "Rejected"
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}