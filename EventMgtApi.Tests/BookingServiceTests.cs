using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Interfaces;
using EventMgtApi.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EventMgtApi.Tests;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock;
    private readonly Mock<IEventRepository> _eventRepoMock;
    private readonly IBookingService _service;

    public BookingServiceTests()
    {
        _bookingRepoMock = new Mock<IBookingRepository>();
        _eventRepoMock = new Mock<IEventRepository>();
        _service = new BookingService(_bookingRepoMock.Object, _eventRepoMock.Object);
    }

    // === УСПЕШНЫЕ СЦЕНАРИИ ===

    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 10);

        _eventRepoMock.Setup(r => r.GetByIdAsync(@event.Id)).ReturnsAsync(@event);
        _bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .Returns((Booking b) => Task.FromResult(b));

        // Act
        var result = await _service.CreateBookingAsync(@event.Id);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be(@event.Id);
        result.Status.Should().Be(BookingStatus.Pending);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllHaveUniqueIds()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 10);

        _eventRepoMock.Setup(r => r.GetByIdAsync(@event.Id)).ReturnsAsync(@event);
        _bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .Returns((Booking b) => Task.FromResult(b));

        // Act
        var b1 = await _service.CreateBookingAsync(@event.Id);
        var b2 = await _service.CreateBookingAsync(@event.Id);

        // Assert
        b1.Id.Should().NotBeEmpty();
        b2.Id.Should().NotBeEmpty();
        b1.Id.Should().NotBe(b2.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsCorrectBooking()
    {
        // Arrange
        var booking = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Pending);

        _bookingRepoMock.Setup(r => r.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(booking.Id);

        // Assert
        result.Should().BeEquivalentTo(booking);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterStatusChange_ReturnsUpdatedStatus()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var pending = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Pending);
        var confirmed = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Confirmed);
        confirmed.Id = pending.Id = bookingId;

        _bookingRepoMock.SetupSequence(r => r.GetByIdAsync(bookingId))
            .ReturnsAsync(pending)
            .ReturnsAsync(confirmed);

        // Act
        var first = await _service.GetBookingByIdAsync(bookingId);
        var second = await _service.GetBookingByIdAsync(bookingId);

        // Assert
        first?.Status.Should().Be(BookingStatus.Pending);
        second?.Status.Should().Be(BookingStatus.Confirmed);
        second?.ProcessedAt.Should().NotBeNull();
    }

    // === СЦЕНАРИИ С ЛОГИКОЙ МЕСТ ===

    [Fact]
    public async Task CreateBookingAsync_ReducesAvailableSeatsByOne()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 10);

        var eventRepo = new InMemoryEventRepository();
        var bookingRepo = new InMemoryBookingRepository();
        var service = new BookingService(bookingRepo, eventRepo);

        await eventRepo.AddAsync(@event);

        // Act — создаем первую бронь
        await service.CreateBookingAsync(@event.Id);

        // Assert
        var updatedEvent = await eventRepo.GetByIdAsync(@event.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public async Task CreateMultipleBookings_UntilLimit_AllSuccessfulWithUniqueIds()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 3);
        var eventRepo = new InMemoryEventRepository();
        var bookingRepo = new InMemoryBookingRepository();
        var service = new BookingService(bookingRepo, eventRepo);

        await eventRepo.AddAsync(@event);

        // Act — создаем 3 брони (до лимита)
        var booking1 = await service.CreateBookingAsync(@event.Id);
        var booking2 = await service.CreateBookingAsync(@event.Id);
        var booking3 = await service.CreateBookingAsync(@event.Id);

        // Assert
        booking1.Id.Should().NotBeEmpty();
        booking2.Id.Should().NotBeEmpty();
        booking3.Id.Should().NotBeEmpty();
        booking1.Id.Should().NotBe(booking2.Id);
        booking2.Id.Should().NotBe(booking3.Id);
        booking1.Id.Should().NotBe(booking3.Id);

        // Проверяем, что места исчерпаны
        var updatedEvent = await eventRepo.GetByIdAsync(@event.Id);
        updatedEvent!.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBooking_AfterExhaustingSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 2);
        var eventRepo = new InMemoryEventRepository();
        var bookingRepo = new InMemoryBookingRepository();
        var service = new BookingService(bookingRepo, eventRepo);

        await eventRepo.AddAsync(@event);

        // Создаем 2 брони, исчерпав места
        await service.CreateBookingAsync(@event.Id);
        await service.CreateBookingAsync(@event.Id);

        // Act & Assert — третья бронь должна упасть
        await service.Invoking(s => s.CreateBookingAsync(@event.Id))
            .Should().ThrowAsync<NoAvailableSeatsException>()
            .WithMessage("Нет доступных мест для данного события.");
    }

    // === НЕУСПЕШНЫЕ СЦЕНАРИИ ===

    [Fact]
    public async Task CreateBookingAsync_NonExistingEvent_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventRepoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

        // Act & Assert
        await _service.Invoking(s => s.CreateBookingAsync(eventId))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Событие с ID {eventId} не найдено.");
    }

    [Fact]
    public async Task CreateBookingAfterDeletingEvent_ThrowsNotFoundException()
    {
        // Arrange: in-memory репозитории
        var eventRepo = new InMemoryEventRepository();
        var bookingRepo = new InMemoryBookingRepository();
        var eventService = new EventService(eventRepo); 
        var bookingService = new BookingService(bookingRepo, eventRepo);

        var @event = TestDataFactory.CreateTestEvent(totalSeats: 10);
        await eventRepo.AddAsync(@event);

        // Убедимся, что можно создать бронь ДО удаления
        var firstBooking = await bookingService.CreateBookingAsync(@event.Id);
        firstBooking.Should().NotBeNull();

        // Проверяем, что бронирование уменьшило AvailableSeats
        var updatedEvent = await eventRepo.GetByIdAsync(@event.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.AvailableSeats.Should().Be(9);

        // Удаляем событие
        await eventService.RemoveEventAsync(@event.Id);

        // Act & Assert: попытка создать бронь после удаления
        await bookingService.Invoking(s => s.CreateBookingAsync(@event.Id))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Событие с ID {@event.Id} не найдено.");
    }

    // Проверяет, что при отсутствии брони выбрасывается NotFoundException
    [Fact]
    public async Task GetBookingByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingBookingId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(nonExistingBookingId)).ReturnsAsync((Booking?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>
                (async () => await _service.GetBookingByIdAsync(nonExistingBookingId));

        Assert.Contains($"Бронь с ID {nonExistingBookingId} не найдена", exception.Message);
    }
}