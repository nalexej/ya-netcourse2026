using EventMgtApi.BookingsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class BookingDbContext : DbContext
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}