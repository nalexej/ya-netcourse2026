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
        var eventId = Guid.NewGuid();
        var @event = new Event() { 
            Id = eventId,
            Title = "Concert",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        _eventRepoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(@event);
        _bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .Returns((Booking b) => Task.FromResult(b));

        // Act
        var result = await _service.CreateBookingAsync(eventId);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be(eventId);
        result.Status.Should().Be(BookingStatus.Pending);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ProcessedAt.Should().BeNull();

        // 🔹 Дополнительно: если бы мы мапили в DTO — проверили бы соответствие
        var responseDto = MapToDto(result);
        responseDto?.Id.Should().Be(result.Id);
        responseDto?.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllHaveUniqueIds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var @event = new Event()
        {
            Id = eventId,
            Title = "Workshop",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        _eventRepoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(@event);
        _bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>()))
            .Returns((Booking b) => Task.FromResult(b));

        // Act
        var b1 = await _service.CreateBookingAsync(eventId);
        var b2 = await _service.CreateBookingAsync(eventId);

        // Assert
        b1.Id.Should().NotBeEmpty();
        b2.Id.Should().NotBeEmpty();
        b1.Id.Should().NotBe(b2.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsCorrectBooking()
    {
        // Arrange
        var booking = new Booking(eventId : Guid.NewGuid())
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAt = null
        };

        _bookingRepoMock.Setup(r => r.GetByIdAsync(booking.Id)).ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(booking.Id);

        // Assert
        result.Should().BeEquivalentTo(booking);

        // 🔹 Проверка маппинга в DTO (на будущее)
        var dto = MapToDto(result);
        dto?.Id.Should().Be(booking.Id);
        dto?.EventId.Should().Be(booking.EventId);
        dto?.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterStatusChange_ReturnsUpdatedStatus()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var pending = new Booking(eventId: Guid.NewGuid())
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAt = null
        };

        var confirmed = new Booking(eventId: Guid.NewGuid())
        {
            Id = Guid.NewGuid(),
            Status = BookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ProcessedAt = DateTime.UtcNow
        };


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

        // 🔹 DTO также отражают изменения
        var dto = MapToDto(second);
        dto?.Status.Should().Be(BookingStatus.Confirmed);
        dto?.ProcessedAt.Should().NotBeNull();
    }

    // === НЕУСПЕШНЫЕ СЦЕНАРИИ ===

    [Fact]
    public async Task CreateBookingAsync_NonExistingEvent_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _eventRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Event?)null);

        // Act & Assert
        await _service.Invoking(s => s.CreateBookingAsync(id))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Событие с ID {id} не найдено.");
    }

    [Fact]
    public async Task CreateBookingAfterDeletingEvent_ThrowsNotFoundException()
    {
        // Arrange: in-memory репозитории
        var eventRepo = new InMemoryEventRepository();
        var bookingRepo = new InMemoryBookingRepository();
        var eventService = new EventService(eventRepo); // если есть сервис удаления
        var bookingService = new BookingService(bookingRepo, eventRepo);

        // Создаём событие
        var eventId = Guid.NewGuid();
        var @event = new Event
        {
            Id = eventId,
            Title = "Concert",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };
        await eventRepo.AddAsync(@event);

        // Убедимся, что можно создать бронь ДО удаления
        var firstBooking = await bookingService.CreateBookingAsync(eventId);
        firstBooking.Should().NotBeNull();

        // Удаляем событие (предположим, что у EventService есть такой метод)
        await eventService.RemoveEventAsync(eventId);

        // Act & Assert: попытка создать бронь после удаления
        await bookingService.Invoking(s => s.CreateBookingAsync(eventId))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Событие с ID {eventId} не найдено.");
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Booking?)null);

        // Act
        var result = await _service.GetBookingByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

    /// <summary>
    /// Имитирует маппинг модели в DTO — как это будет в контроллере.
    /// Показывает, что данные корректны для передачи клиенту.
    /// </summary>
    private BookingResponseDto? MapToDto(Booking? booking)
    {
        if (booking == null) return null;

        return new BookingResponseDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}