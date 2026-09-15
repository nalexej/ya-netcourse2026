
namespace EventMgtApi.Contracts.Events.DTOs;

/// <summary>
/// Результат запроса «топ-N событий по проценту продаж».
/// </summary>
public sealed class TopEventDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required int TotalSeats { get; set; }
    public required int AvailableSeats { get; set; }
    public required decimal SoldPercent { get; set; }
}