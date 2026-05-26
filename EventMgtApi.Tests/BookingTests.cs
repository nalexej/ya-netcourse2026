using EventMgtApi.Models;
using FluentAssertions;
using System;
using Xunit;

namespace EventMgtApi.Tests;

public class BookingTests
{
    private readonly Guid _eventId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithEventId_SetsDefaults()
    {
        // Act
        var booking = new Booking(_eventId);

        // Assert
        booking.EventId.Should().Be(_eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.Id.Should().NotBeEmpty();
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void ManualConfirm_SetsStatusToConfirmedAndProcessedAt()
    {
        // Arrange
        var booking = new Booking(_eventId);

        // Act
        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = DateTime.UtcNow;

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ManualReject_SetsStatusToRejectedAndProcessedAt()
    {
        // Arrange
        var booking = new Booking(_eventId);

        // Act
        booking.Status = BookingStatus.Rejected;
        booking.ProcessedAt = DateTime.UtcNow;

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}