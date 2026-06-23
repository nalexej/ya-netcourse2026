using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Infrastructure.DataAccess;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventMgtApi.Tests;

public sealed class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public BookingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private async Task<EventDtoResponse> CreateTestEventAsync(int totalSeats = 10)
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var created = await _eventService.AddEventAsync(new EventDto
        {
            Title = "Test Event",
            StartAt = futureDate,
            EndAt = futureDate.AddHours(2),
            TotalSeats = totalSeats
        });
        return created;
    }

    // === УСПЕШНЫЕ СЦЕНАРИИ ===

    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        var @event = await CreateTestEventAsync(totalSeats: 10);

        // Act
        var result = await _bookingService.CreateBookingAsync(@event.Id);

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
        var @event = await CreateTestEventAsync(totalSeats: 10);

        // Act
        var b1 = await _bookingService.CreateBookingAsync(@event.Id);
        var b2 = await _bookingService.CreateBookingAsync(@event.Id);

        // Assert
        b1.Id.Should().NotBeEmpty();
        b2.Id.Should().NotBeEmpty();
        b1.Id.Should().NotBe(b2.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsCorrectBooking()
    {
        // Arrange
        var @event = await CreateTestEventAsync(totalSeats: 10);
        var booking = await _bookingService.CreateBookingAsync(@event.Id);

        // Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        // Assert
        result.Should().BeEquivalentTo(booking);
    }


    // === СЦЕНАРИИ С ЛОГИКОЙ МЕСТ ===

    [Fact]
    public async Task CreateBookingAsync_ReducesAvailableSeatsByOne()
    {
        // Arrange
        var @event = await CreateTestEventAsync(totalSeats: 10);

        // Act — создаем первую бронь
        var result = await _bookingService.CreateBookingAsync(@event.Id);

        // Assert
        var updatedEvent = await _eventService.GetEventAsync(@event.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public async Task CreateMultipleBookings_UntilLimit_AllSuccessfulWithUniqueIds()
    {
        // Arrange
        var @event = await CreateTestEventAsync(totalSeats: 3);

        // Act — создаем 3 брони (до лимита)
        var booking1 = await _bookingService.CreateBookingAsync(@event.Id);
        var booking2 = await _bookingService.CreateBookingAsync(@event.Id);
        var booking3 = await _bookingService.CreateBookingAsync(@event.Id);

        // Assert
        booking1.Id.Should().NotBeEmpty();
        booking2.Id.Should().NotBeEmpty();
        booking3.Id.Should().NotBeEmpty();
        booking1.Id.Should().NotBe(booking2.Id);
        booking2.Id.Should().NotBe(booking3.Id);
        booking1.Id.Should().NotBe(booking3.Id);

        // Проверяем, что места исчерпаны
        var updatedEvent = await _eventService.GetEventAsync(@event.Id);
        updatedEvent!.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBooking_AfterExhaustingSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var @event = await CreateTestEventAsync(totalSeats: 2);

        // Создаем 2 брони, исчерпав места
        var booking1 = await _bookingService.CreateBookingAsync(@event.Id);
        var booking2 = await _bookingService.CreateBookingAsync(@event.Id);

        // Act & Assert — третья бронь должна упасть
        await _bookingService.Invoking(s => s.CreateBookingAsync(@event.Id))
            .Should().ThrowAsync<NoAvailableSeatsException>()
            .WithMessage("Нет доступных мест для данного события.");
    }

    // === НЕУСПЕШНЫЕ СЦЕНАРИИ ===

    [Fact]
    public async Task CreateBookingAsync_NonExistingEvent_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act & Assert
        await _bookingService.Invoking(s => s.CreateBookingAsync(eventId))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Событие с ID {eventId} не найдено.");
    }

    [Fact]
    public async Task CreateBookingAfterDeletingEvent_ThrowsNotFoundException()
    {
        // Arrange
        var @event = await CreateTestEventAsync(totalSeats: 10);

        // Убедимся, что можно создать бронь ДО удаления
        var firstBooking = await _bookingService.CreateBookingAsync(@event.Id);
        firstBooking.Should().NotBeNull();

        // Проверяем, что бронирование уменьшило AvailableSeats
        var updatedEvent = await _eventService.GetEventAsync(@event.Id);
        updatedEvent.Should().NotBeNull();
        updatedEvent!.AvailableSeats.Should().Be(9);

        // Удаляем событие
        await _eventService.RemoveEventAsync(@event.Id);

        // Act & Assert: попытка создать бронь после удаления
        await _bookingService.Invoking(s => s.CreateBookingAsync(@event.Id))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Событие с ID {@event.Id} не найдено.");
    }

    // Проверяет, что при отсутствии брони выбрасывается NotFoundException
    [Fact]
    public async Task GetBookingByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingBookingId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>
                (async () => await _bookingService.GetBookingByIdAsync(nonExistingBookingId));

        Assert.Contains($"Бронь с ID {nonExistingBookingId} не найдена", exception.Message);
    }
}