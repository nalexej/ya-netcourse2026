using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;

namespace EventMgtApi.Tests;

public static class TestDataFactory
{
    public static Event CreateTestEvent(
        int totalSeats = 10,
        int? availableSeats = null, // null = по умолчанию = TotalSeats
        string? title = null,
        DateTime? startAt = null,
        DateTime? endAt = null,
        string? description = null)
    {
        var @event = Event.Create(
            title: title ?? "Test Event",
            startAt: startAt ?? DateTime.UtcNow.AddHours(1),
            endAt: endAt ?? DateTime.UtcNow.AddHours(2),
            totalSeats: totalSeats,
            description: description
        );

        if (availableSeats.HasValue)
        {
            @event.AvailableSeats = availableSeats.Value;
        }

        return @event;
    }

    public static Booking CreateBooking(Guid eventId, Guid userId = default, BookingStatus status = BookingStatus.Pending)
    {
        return new Booking(eventId, userId)
        {
            Status = status,
            ProcessedAt = status switch
            {
                BookingStatus.Confirmed or BookingStatus.Cancelled or BookingStatus.Rejected => DateTime.UtcNow,
                _ => null
            }
        };
    }
}