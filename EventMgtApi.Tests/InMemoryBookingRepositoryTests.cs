using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EventMgtApi.Tests;
public class InMemoryBookingRepositoryTests
{
    private readonly InMemoryBookingRepository _repo = new();
    private readonly Guid _eventId = Guid.NewGuid();

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ReturnsSameBooking()
    {
        // Arrange
        var booking = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Pending);

        // Act
        await _repo.AddAsync(booking);
        var result = await _repo.GetByIdAsync(booking.Id);

        // Assert
        result.Should().BeEquivalentTo(booking);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingBooking()
    {
        // Arrange
        var booking = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Pending);

        await _repo.AddAsync(booking);

        booking.Status = BookingStatus.Confirmed;
        booking.ProcessedAt = DateTime.UtcNow;

        // Act
        var result = await _repo.UpdateAsync(booking);
        var updated = await _repo.GetByIdAsync(booking.Id);

        // Assert
        result.Should().BeTrue();
        updated?.Status.Should().Be(BookingStatus.Confirmed);
        updated?.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEventIdAsync_ReturnsBookingsForEvent()
    {
        // Arrange
        var booking1 = TestDataFactory.CreateBooking(_eventId, BookingStatus.Pending);
        var booking2 = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Pending);

        await _repo.AddAsync(booking1);
        await _repo.AddAsync(booking2);

        // Act
        var result = await _repo.GetByEventIdAsync(_eventId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainSingle(b => b.Id == booking1.Id);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsBookingsWithSpecificStatus()
    {
        // Arrange
        var booking1 = TestDataFactory.CreateBooking(_eventId, BookingStatus.Pending);
        var booking2 = TestDataFactory.CreateBooking(_eventId, BookingStatus.Confirmed);
        var booking3 = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Pending);

        await _repo.AddAsync(booking1);
        await _repo.AddAsync(booking2);
        await _repo.AddAsync(booking3);

        // Act
        var result = await _repo.GetByStatusAsync(BookingStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Id == booking1.Id);
        result.Should().Contain(b => b.Id == booking3.Id);
        result.Should().NotContain(b => b.Id == booking2.Id);
    }
}
