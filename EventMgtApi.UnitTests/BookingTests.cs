using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EventMgtApi.Tests;

public class BookingTests
{
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public void Confirm_SetsStatusToConfirmed(BookingStatus initialState)
    {
        // Arrange
        var booking = TestDataFactory.CreateBooking(Guid.NewGuid(), initialState);

        // Act
        booking.Confirm();

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed,
            $"Confirm() должен установить статус Confirmed для начального состояния {initialState}");

        booking.ProcessedAt.Should().NotBeNull("ProcessedAt должен быть установлен");
        booking.ProcessedAt!.Value.Kind.Should().Be(DateTimeKind.Utc, "ProcessedAt должен быть в UTC");
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public void Reject_SetsStatusToRejected(BookingStatus initialState)
    {
        // Arrange
        var booking = TestDataFactory.CreateBooking(Guid.NewGuid(), initialState);

        // Act
        booking.Reject();

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected,
            $"Reject() должен установить статус Rejected для начального состояния {initialState}");

        booking.ProcessedAt.Should().NotBeNull("ProcessedAt должен быть установлен");
        booking.ProcessedAt!.Value.Kind.Should().Be(DateTimeKind.Utc, "ProcessedAt должен быть в UTC");
    }
}