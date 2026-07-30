using EventMgtApi.Application.Interfaces;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using FluentAssertions;
using Moq;


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
        _service = new BookingService(_eventRepoMock.Object, _bookingRepoMock.Object);
    }

    // === УСПЕШНЫЕ СЦЕНАРИИ ===

    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 10);

        _eventRepoMock.Setup(r => r.GetByIdAsync(@event.Id)).ReturnsAsync(@event);
        _bookingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
        _bookingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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

        _bookingRepoMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(booking.Id);

        // Assert — сравниваем ПО ПОЛЯМ, исключая Event (навигационное)
        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(booking.EventId);
        result.Status.Should().Be(booking.Status);
        result.CreatedAt.Should().BeCloseTo(booking.CreatedAt, TimeSpan.FromMinutes(1));
        result.ProcessedAt.Should().Be(booking.ProcessedAt);
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

    [Fact]
    public async Task CreateMultipleBookings_UntilLimit_SuccessfullyWithUniqueIds()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 3); // AvailableSeats не важно — он вычисляется

        _eventRepoMock.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _bookingRepoMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var b1 = await _service.CreateBookingAsync(@event.Id);
        var b2 = await _service.CreateBookingAsync(@event.Id);
        var b3 = await _service.CreateBookingAsync(@event.Id);

        // Assert — все брони уникальны
        Assert.NotEqual(Guid.Empty, b1.Id);
        Assert.NotEqual(Guid.Empty, b2.Id);
        Assert.NotEqual(Guid.Empty, b3.Id);
        Assert.NotEqual(b1.Id, b2.Id);
        Assert.NotEqual(b2.Id, b3.Id);

        // 🔥 Проверяем, что AddAsync вызван 3 раза, и статус = Pending
        _bookingRepoMock.Verify(r => r.AddAsync(
            It.Is<Booking>(b => b.Status == BookingStatus.Pending),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateBooking_AfterExhaustingSeats_ThrowsNoAvailableSeatsException()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 2);
        var eventRepoId = @event.Id;

        // Мокаем GetByIdAsync 3 раза (для каждой брони)
        _eventRepoMock
            .SetupSequence(r => r.GetByIdAsync(eventRepoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event) // 1-я бронь
            .ReturnsAsync(@event) // 2-я бронь
            .ReturnsAsync(@event); // 3-я бронь

        // Мокаем AddAsync (для успешных броней)
        _bookingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act & Assert — первые 2 брони успешны
        await _service.CreateBookingAsync(@event.Id);
        await _service.CreateBookingAsync(@event.Id);

        // Третья бронь должна бросить NoAvailableSeatsException
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(@event.Id));
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
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 10);

        _eventRepoMock
            .Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Сначала убеждаемся, что бронь создаётся ДО удаления
        _bookingRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act 1: первая бронь — должна пройти
        var firstBooking = await _service.CreateBookingAsync(@event.Id);
        firstBooking.Should().NotBeNull();
        firstBooking.EventId.Should().Be(@event.Id);

        // Act 2: удаляем событие — имитируем, что GetByIdAsync теперь возвращает null
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null); // ← теперь событие "удалено"

        // Assert: вторая попытка брони — должна бросить NotFoundException
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateBookingAsync(@event.Id));
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