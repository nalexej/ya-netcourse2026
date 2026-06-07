using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace EventMgtApi.Tests.Unit;

/// <summary>
/// Unit-тесты для Event — только на текущую реализацию (без lock, без private set).
/// </summary>
public class EventTests
{
    [Fact]
    public void Event_TryReserveSeats_SingleThread_ShouldReduceAvailableSeats()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 5);
        var initialAvailable = @event.AvailableSeats;

        // Act
        var result = @event.TryReserveSeats();

        // Assert
        result.Should().BeTrue();
        @event.AvailableSeats.Should().Be(initialAvailable - 1,
            "Доступных мест должно уменьшиться на 1 при успешной резервации.");
    }

    [Fact]
    public void Event_TryReserveSeats_RejectsWhenNoSeats()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 2);
        @event.TryReserveSeats().Should().BeTrue(); // 1
        @event.TryReserveSeats().Should().BeTrue(); // 2

        // Act & Assert
        @event.AvailableSeats.Should().Be(0);
        @event.TryReserveSeats().Should().BeFalse("мест больше нет");
        @event.AvailableSeats.Should().Be(0, "список не меняется при отказе");
    }

    [Fact]
    public void Event_ReleaseSeats_SingleThread_ShouldIncreaseAvailableSeatsButNotExceedTotal()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 5);
        @event.AvailableSeats = 0; // Занять все места вручную (тест на оригинальный код)

        // Act
        @event.ReleaseSeats(3);

        // Assert
        @event.AvailableSeats.Should().Be(3, "должно вернуться 3 места");
        @event.AvailableSeats.Should().BeLessThanOrEqualTo(@event.TotalSeats,
            "доступных мест не может быть больше TotalSeats");
    }

    [Fact]
    public void Event_ReleaseSeats_DoesNotExceedTotal_WithExtraReleases()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 3);
        @event.AvailableSeats = 0; // Занять все места вручную

        // Act: попытка вернуть 5 мест (но всего 3)
        @event.ReleaseSeats(5);

        // Assert
        @event.AvailableSeats.Should().Be(3,
            "даже при 5 ReleaseSeats, доступных мест не может быть больше TotalSeats");
    }

    [Fact]
    public void Event_ReleaseSeats_WithZeroCount_DoesNothing()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 5);
        @event.AvailableSeats = 2;

        // Act
        @event.ReleaseSeats(0);

        // Assert
        @event.AvailableSeats.Should().Be(2, "с нулем ничего не меняется");
    }

    [Fact]
    public void Event_ReleaseSeats_WithNegativeCount_DoesNothing()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 5);
        @event.AvailableSeats = 3;

        // Act
        @event.ReleaseSeats(-5);

        // Assert
        @event.AvailableSeats.Should().Be(3, "Отрицательное значение не меняет AvailableSeats");
    }

    [Fact]
    public void Event_TryReserveSeats_WithZeroCount_ShouldPass()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 5);

        // Act
        var result = @event.TryReserveSeats(0);

        // Assert
        result.Should().BeTrue("0 мест — это не ошибка");
        @event.AvailableSeats.Should().Be(@event.TotalSeats, "AvailableSeats не меняется");
    }

    // Тест на сценарий из фонового сервиса: Reject + ReleaseSeats
    [Fact]
    public void Event_ReleaseSeats_AfterReject_PreservesIntegrity()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 3);
        @event.AvailableSeats = 0; // Занять все места (симуляция бронирования)

        // Act: отмена — вернуть 1 место (как в фоновом сервисе)
        @event.ReleaseSeats(1);

        // Assert
        @event.AvailableSeats.Should().Be(1, "После ReleaseSeats(1) должно вернуться 1 место");
    }

}
