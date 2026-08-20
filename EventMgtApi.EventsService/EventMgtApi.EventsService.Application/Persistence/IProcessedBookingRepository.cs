namespace EventMgtApi.EventsService.Application.Persistence;

public interface IProcessedBookingRepository
{
    Task<bool> ExistsAsync(Guid eventId, Guid bookingId, string eventType, CancellationToken ct = default);
    Task AddAsync(Guid eventId, Guid bookingId, string eventType, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}