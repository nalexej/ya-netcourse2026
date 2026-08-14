using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.Interfaces;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Options;
using EventMgtApi.Infrastructure.BackgroundServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;


namespace EventMgtApi.Tests;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepoMock;
    private readonly Mock<IEventRepository> _eventRepoMock;
    private readonly Mock<IOptions<BookingOptions>> _bookingOptionsMock;
    private readonly IBookingService _service;

    public BookingServiceTests()
    {
        _bookingRepoMock = new Mock<IBookingRepository>();
        _eventRepoMock = new Mock<IEventRepository>();
        _bookingOptionsMock = new Mock<IOptions<BookingOptions>>();
        _bookingOptionsMock.Setup(m => m.Value).Returns(new BookingOptions { MaxActiveBookings = 10 });

        _service = new BookingService(_eventRepoMock.Object, _bookingRepoMock.Object, _bookingOptionsMock.Object);
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
        var result = await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());

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
        var b1 = await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());
        var b2 = await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());

        // Assert
        b1.Id.Should().NotBeEmpty();
        b2.Id.Should().NotBeEmpty();
        b1.Id.Should().NotBe(b2.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsCorrectBooking()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var booking = TestDataFactory.CreateBooking(bookingId, userId, BookingStatus.Pending);

        _bookingRepoMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _service.GetBookingByIdAsync(booking.Id, userId, false);

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
        var userId = Guid.NewGuid();
        var pending = TestDataFactory.CreateBooking(Guid.NewGuid(), userId, BookingStatus.Pending);
        var confirmed = TestDataFactory.CreateBooking(Guid.NewGuid(), userId, BookingStatus.Confirmed);
        confirmed.Id = pending.Id = bookingId;

        _bookingRepoMock.SetupSequence(r => r.GetByIdAsync(bookingId))
            .ReturnsAsync(pending)
            .ReturnsAsync(confirmed);

        // Act
        var first = await _service.GetBookingByIdAsync(bookingId, userId, false);
        var second = await _service.GetBookingByIdAsync(bookingId, userId, false);

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
        var b1 = await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());
        var b2 = await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());
        var b3 = await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());

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
        await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());
        await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());

        // Третья бронь должна бросить NoAvailableSeatsException
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _service.CreateBookingAsync(@event.Id, Guid.NewGuid()));
    }

    /// <summary>
    /// Тест: Администратор может запросить бронь любого пользователя.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_AdminCanRequestAnyBooking_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid(); // Владелец брони
        var adminId = Guid.NewGuid();     // Администратор (запрашивает)
        var isAdmin = true;

        // Создаем мок бронирования, принадлежащего другому пользователю
        var booking = TestDataFactory.CreateBooking(eventId, ownerUserId, BookingStatus.Confirmed);

        _bookingRepoMock
            .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Инициализируем сервис с настройками (если они нужны, либо берем из конструктора)
        var service = new BookingService(
            _eventRepoMock.Object,
            _bookingRepoMock.Object,
            _bookingOptionsMock.Object
        );

        // Act
        var result = await service.GetBookingByIdAsync(booking.Id, adminId, isAdmin);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(booking.Id);
        result.UserId.Should().Be(ownerUserId); // Убеждаемся, что вернулась именно эта бронь
    }


    // === НЕУСПЕШНЫЕ СЦЕНАРИИ ===

    [Fact]
    public async Task CreateBookingAsync_NonExistingEvent_ThrowsNotFoundException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _eventRepoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((Event?)null);

        // Act & Assert
        await _service.Invoking(s => s.CreateBookingAsync(eventId, Guid.NewGuid()))
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
        var firstBooking = await _service.CreateBookingAsync(@event.Id, Guid.NewGuid());
        firstBooking.Should().NotBeNull();
        firstBooking.EventId.Should().Be(@event.Id);

        // Act 2: удаляем событие — имитируем, что GetByIdAsync теперь возвращает null
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null); // ← теперь событие "удалено"

        // Assert: вторая попытка брони — должна бросить NotFoundException
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateBookingAsync(@event.Id, Guid.NewGuid()));
    }

    // Проверяет, что при отсутствии брони выбрасывается NotFoundException
    [Fact]
    public async Task GetBookingByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistingBookingId = Guid.NewGuid();
        _bookingRepoMock.Setup(r => r.GetByIdAsync(nonExistingBookingId)).ReturnsAsync((Booking?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>
                (async () => await _service.GetBookingByIdAsync(nonExistingBookingId, userId, false));

        Assert.Contains($"Бронь с ID {nonExistingBookingId} не найдена", exception.Message);
    }

    /// <summary>
    /// Тест: попытка забронировать прошедшее событие должна выбросить BookingPastEventException.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_PastEvent_ShouldThrowException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 1. Создаем экземпляр через приватный конструктор
        var constructor = typeof(Event).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        var pastEvent = (Event)constructor.Invoke(new object[] { });

        // 2. Устанавливаем значения свойств
        typeof(Event).GetProperty("Id")!.SetValue(pastEvent, eventId);
        typeof(Event).GetProperty("Title")!.SetValue(pastEvent, "Past Event");
        typeof(Event).GetProperty("StartAt")!.SetValue(pastEvent, DateTime.UtcNow.AddHours(-2));
        typeof(Event).GetProperty("EndAt")!.SetValue(pastEvent, DateTime.UtcNow.AddHours(-1));
        typeof(Event).GetProperty("TotalSeats")!.SetValue(pastEvent, 10);
        typeof(Event).GetProperty("AvailableSeats")!.SetValue(pastEvent, 10);
        typeof(Event).GetProperty("Bookings")!.SetValue(pastEvent, new List<Booking>());

        // 3. Устанавливаем мок репозитория
        _eventRepoMock.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(pastEvent);

        // 4. Запускаем тест
        await Assert.ThrowsAsync<BookingPastEventException>(() =>
            _service.CreateBookingAsync(eventId, userId));
    }

    /// <summary>
    /// Тест: попытка создать бронь при достижении лимита активных броней должна выбросить TooManyActiveBookingsException.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_MaxActiveBookingsLimitReached_ShouldThrowException()
    {
        // Arrange
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 15);
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Устанавливаем лимит
        _bookingOptionsMock.Setup(m => m.Value).Returns(new BookingOptions { MaxActiveBookings = 10 });

        _eventRepoMock.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _bookingRepoMock.Setup(repo => repo.GetActiveBookingsCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TooManyActiveBookingsException>(async () =>
            await _service.CreateBookingAsync(eventId, userId));
    }

    /// <summary>
    /// Тест: лимиты разных пользователей независимы.
    /// Если у одного пользователя лимит исчерпан, у другого с таким же лимитом можно создавать брони.
    /// </summary>
    [Fact]
    public async Task CreateBookingAsync_DifferentUsersLimitsShouldNotAffectEachOther_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Пользователь, у которого лимит еще не исчерпан
        var userAId = Guid.NewGuid();
        // Пользователь, у которого лимит уже исчерпан (для симуляции, что другие пользуются сервисом)
        var userBId = Guid.NewGuid();

        // Устанавливаем лимит
        _bookingOptionsMock.Setup(m => m.Value).Returns(new BookingOptions { MaxActiveBookings = 10 });

        // 1. Создаем валидное будущее событие
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 15);

        // Настраиваем моки для событийного репозитория
        _eventRepoMock.Setup(repo => repo.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // 2. Настраиваем моки для репозитория броней:
        // У пользователя A (который не бронировал) - 0 активных броней.
        _bookingRepoMock.Setup(repo => repo.GetActiveBookingsCountAsync(userAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // У пользователя B (для контекста) - 10 активных броней (лимит исчерпан).
        _bookingRepoMock.Setup(repo => repo.GetActiveBookingsCountAsync(userBId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        // Пытаемся создать бронь для пользователя A, у которого лимит свободен
        var result = await _service.CreateBookingAsync(eventId, userAId);

        // Assert
        // Бронь должна вернуться успешно, исключений быть не должно
        Assert.NotNull(result);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(userAId, result.UserId);

        // Дополнительная проверка: убедиться, что репозиторий попытался сохранить бронь
        _bookingRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _bookingRepoMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Тест: Пользователь не может отменить чужую бронь.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_NotOwner_ShouldThrowForbiddenException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var requestUserId = Guid.NewGuid(); // Не владелец
        var isAdmin = false;

        // Создаем событие в будущем, чтобы не выбросить BookingPastEventException
        var @event = TestDataFactory.CreateTestEvent(
            totalSeats: 10,
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2)
        );

        // Создаем бронь с помощью фабрики
        var booking = TestDataFactory.CreateBooking(
            eventId: @event.Id,
            userId: ownerId,
            status: BookingStatus.Confirmed
        );

        _bookingRepoMock.Setup(repo => repo.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.CancelBookingAsync(booking.Id, requestUserId, isAdmin));

        Assert.Equal("Недостаточно прав для отмены этой брони. Вы можете отменить только свою бронь.", exception.Message);
    }

    /// <summary>
    /// Тест: Администратор может отменить бронь любого пользователя.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_AdminCanCancelAnyBooking_ShouldSucceed()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var isAdmin = true;

        var @event = TestDataFactory.CreateTestEvent(
            totalSeats: 10,
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2)
        );

        var booking = TestDataFactory.CreateBooking(
            eventId: @event.Id,
            userId: ownerId,
            status: BookingStatus.Confirmed
        );

        _bookingRepoMock.Setup(repo => repo.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _eventRepoMock.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(@event);

        // Act
        var result = await _service.CancelBookingAsync(booking.Id, adminId, isAdmin);

        // Assert
        Assert.NotNull(result);
        // Проверяем, что статус изменился на Cancelled
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    /// <summary>
    /// Тест: Владелец может успешно отменить свою бронь на будущее событие.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_OwnerCanCancelOwnBooking_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var isAdmin = false;

        var @event = TestDataFactory.CreateTestEvent(
            totalSeats: 10,
            startAt: DateTime.UtcNow.AddHours(2),
            endAt: DateTime.UtcNow.AddHours(3)
        );

        var booking = TestDataFactory.CreateBooking(
            eventId: @event.Id,
            userId: userId,
            status: BookingStatus.Confirmed
        );

        _bookingRepoMock.Setup(repo => repo.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _eventRepoMock.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(@event);

        // Act
        var result = await _service.CancelBookingAsync(booking.Id, userId, isAdmin);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        _bookingRepoMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Тест: При отмене брони количество доступных мест на событии увеличивается.
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldIncreaseAvailableSeats_WhenBookingIsCancelled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var isAdmin = false;

        // Исходное количество мест
        var initialAvailableSeats = 5;

        // 1. Создаем событие с известным количеством мест
        var @event = TestDataFactory.CreateTestEvent(
            totalSeats: 10,
            startAt: DateTime.UtcNow.AddHours(1), // Будущее событие
            endAt: DateTime.UtcNow.AddHours(2),
            availableSeats: initialAvailableSeats
        );

        // 2. Создаем бронь
        var booking = TestDataFactory.CreateBooking(
            eventId: @event.Id,
            userId: userId,
            status: BookingStatus.Confirmed
        );

        booking.EventId = @event.Id;

        // 3. Настраиваем моки репозиториев
        _bookingRepoMock.Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _bookingRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventRepoMock.Setup(r => r.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _eventRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CancelBookingAsync(booking.Id, userId, isAdmin);

        // Assert
        // 1. Проверяем, что AvailableSeats увеличился на 1
        @event.AvailableSeats.Should().Be(initialAvailableSeats + 1);

        // 2. Проверяем, что статус бронирования стал Cancelled
        booking.Status.Should().Be(BookingStatus.Cancelled);

        // 3. Проверяем, что методы сохранения были вызваны
        _bookingRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Тест: При ошибке обработки брони в BackgroundService место должно освободиться.
    /// </summary>
    [Fact]
    public async Task BackgroundService_ProcessBooking_OnError_ShouldReleaseSeats()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Создаем событие с 9 свободными местами (1 место "занято" этой бронью)
        var testEvent = TestDataFactory.CreateTestEvent(
            totalSeats: 10,
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            availableSeats: 9
        );
        var eventId = testEvent.Id;

        var testBooking = new Booking(eventId, userId)
        {
            Id = bookingId,
            Status = BookingStatus.Pending
        };

        // 1. Моки BookingRepo
        _bookingRepoMock
            .Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testBooking);

        int bookingSaveChangesCallCount = 0;
        _bookingRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => bookingSaveChangesCallCount++)
            .Returns(async () =>
            {
                // Первый вызов (в try блоке) выбрасывает ошибку
                if (bookingSaveChangesCallCount == 1)
                {
                    throw new InvalidOperationException("Simulated error in try");
                }
                return Task.CompletedTask;
            });

        // 2. Моки EventRepo
        // Счетчик вызовов GetByIdAsync для проверки логики восстановления
        int eventGetByIdCallCount = 0;
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testEvent)
            .Callback(() => eventGetByIdCallCount++);

        // SaveChanges для EventRepo всегда успешен
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // 3. DI Setup
        var services = new ServiceCollection();
        services.AddSingleton<IEventRepository>(_eventRepoMock.Object);
        services.AddSingleton<IBookingRepository>(_bookingRepoMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(serviceProvider);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var logger = new LoggerFactory().CreateLogger<BookingProcessingBackgroundService>();
        var service = new BookingProcessingBackgroundService(mockScopeFactory.Object, logger);

        var method = typeof(BookingProcessingBackgroundService).GetMethod(
            "ProcessBookingAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        var task = (Task)method!.Invoke(service, new object[] { bookingId, cts.Token })!;
        await task;

        // Assert
        // 1. Броня должна быть отклонена
        testBooking.Status.Should().Be(BookingStatus.Rejected);

        // 2. Место должно вернуться в пул (было 9, стало 10)
        testEvent.AvailableSeats.Should().Be(10);

        // 3. SaveChanges BookingRepo был вызван 1 раз (ошибка в try, во время восстановления он не сохраняется)
        bookingSaveChangesCallCount.Should().Be(1);

        // 4. EventRepo GetById вызывался хотя бы 1 раз (в try и в catch восстановления)
        eventGetByIdCallCount.Should().BeGreaterThanOrEqualTo(1);

        // 5. EventRepo SaveChanges должен быть вызван для сохранения изменения мест
        _eventRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Тест: Обычный пользователь не может запросить чужую бронь.
    /// </summary>
    [Fact]
    public async Task GetBookingByIdAsync_UserNotOwnerAndNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid(); // Пользователь, который делает запрос (не владелец)
        var isAdmin = false;

        var booking = TestDataFactory.CreateBooking(bookingId, ownerUserId, BookingStatus.Confirmed);

        _bookingRepoMock
            .Setup(r => r.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var service = new BookingService(
            _eventRepoMock.Object,
            _bookingRepoMock.Object,
            _bookingOptionsMock.Object
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetBookingByIdAsync(bookingId, otherUserId, isAdmin));

        exception.Message.Should().Contain("Недостаточно прав");
    }

}