using EventMgtApi.EventsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.EventsService.Infrastructure.Persistence;
public class EventDbContext: DbContext
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<ProcessedBooking> ProcessedBookings => Set<ProcessedBooking>();
    public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) {}
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventDbContext).Assembly);
    }
}