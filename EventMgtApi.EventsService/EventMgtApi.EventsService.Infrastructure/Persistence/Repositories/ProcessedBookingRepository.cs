using EventMgtApi.EventsService.Application.Persistence;
using EventMgtApi.EventsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.EventsService.Infrastructure.Persistence.Repositories;

public class ProcessedBookingRepository : IProcessedBookingRepository
{
    private readonly EventDbContext _dbContext;

    public ProcessedBookingRepository(EventDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsAsync(Guid eventId, Guid bookingId, string eventType, CancellationToken ct = default)
    {
        return await _dbContext.ProcessedBookings
            .AnyAsync<ProcessedBooking>(pb => pb.EventId == eventId && pb.BookingId == bookingId && pb.EventType == eventType, ct);
    }

    public async Task AddAsync(Guid eventId, Guid bookingId, string eventType, CancellationToken ct = default)
    {
        var processed = new ProcessedBooking
        {
            EventId = eventId,
            BookingId = bookingId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        };

        _dbContext.ProcessedBookings.Add(processed);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}