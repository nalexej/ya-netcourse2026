using EventMgtApi.EventsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventMgtApi.EventsService.Infrastructure.Persistence.Configurations;

public class ProcessedBookingConfiguration : IEntityTypeConfiguration<ProcessedBooking>
{
    public void Configure(EntityTypeBuilder<ProcessedBooking> builder)
    {
        builder.ToTable("ProcessedBookings");

        builder.HasKey(pb => pb.Id);

        // Уникальный индекс по EventId + BookingId + EventType — не может быть дубликатов
        builder.HasIndex(pb => new { pb.EventId, pb.BookingId, pb.EventType })
            .IsUnique()
            .HasDatabaseName("IX_ProcessedBookings_EventId_BookingId_EventType");

        builder.Property(pb => pb.EventType)
            .IsRequired()
            .HasMaxLength(50);
    }
}