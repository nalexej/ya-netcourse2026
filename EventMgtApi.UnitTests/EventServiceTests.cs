using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Interfaces;
using EventMgtApi.Infrastructure.DataAccess;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EventMgtApi.Tests;

public sealed class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    private readonly Mock<IBookingRepository> _bookingRepoMock;
    private readonly Mock<IEventRepository> _eventRepoMock;
    private readonly IEventService _eventService;
    private readonly IBookingService _bookindService;

    public EventServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        _bookingRepoMock = new Mock<IBookingRepository>();
        _eventRepoMock = new Mock<IEventRepository>();
        _bookindService = new BookingService(_eventRepoMock.Object, _bookingRepoMock.Object);
        _eventService = new EventService(_eventRepoMock.Object);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    // Тестовые данные: валидные DTO для создания/обновления событий
    public static IEnumerable<object[]> ValidEventDtos()
    {
        yield return new object[]
        {
            new EventDto
            {
                Title = "Встреча с командой (план)",
                Description = "Обсуждение плана",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(2),
                TotalSeats = 10
            },
            "Обсуждение плана"
        };

        yield return new object[]
        {
            new EventDto
            {
                Title = "План на квартал",
                Description = null,
                StartAt = DateTime.UtcNow.AddDays(3),
                EndAt = DateTime.UtcNow.AddDays(4),
                TotalSeats = 10
            },
            string.Empty
        };

        yield return new object[]
        {
            new EventDto
            {
                Title = "Встреча по обучению сотрудников",
                StartAt = DateTime.UtcNow.AddDays(5),
                EndAt = DateTime.UtcNow.AddDays(6),
                TotalSeats = 10
            },
            string.Empty
        };
    }

    // ========================================================================
    // 1. Создание событий
    // ========================================================================
    // Проверяет, что валидное событие создаётся с корректным ID и полями

    [Theory]
    [MemberData(nameof(ValidEventDtos))]
    public async Task CreateEvent_ValidEvent_ReturnsCreatedEventWithId(
        EventDto inputEvent,
        string expectedDescription)
    {
        // Arrange
        var @expectedEvent = TestDataFactory.CreateTestEvent(title: inputEvent!.Title!.Trim(), startAt: inputEvent.StartAt, endAt: inputEvent.EndAt, totalSeats: 2, description: expectedDescription);

        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _eventService.AddEventAsync(inputEvent);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be(inputEvent.Title);
        result.Description.Should().Be(expectedDescription);
        result.StartAt.Should().Be(inputEvent.StartAt);
        result.EndAt.Should().Be(inputEvent.EndAt);

        // Проверяем, что репозиторий вызван один раз с валидным событием
        _eventRepoMock.Verify(r => r.AddAsync(
            It.Is<Event>(e =>
                e.Title == inputEvent.Title &&
                e.TotalSeats == inputEvent.TotalSeats &&
                e.AvailableSeats == inputEvent.TotalSeats),
            It.IsAny<CancellationToken>()), Times.Once);

        _eventRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEvent_WithNullTitle_ThrowsValidationException()
    {
        // Arrange
        var invalidDto = new EventDto
        {
            Title = null,
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.AddEventAsync(invalidDto));
    }

    [Fact]
    public async Task CreateEvent_WithZeroTotalSeats_ThrowsValidationException()
    {
        // Arrange
        var invalidDto = new EventDto
        {
            Title = "Test",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 0
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.AddEventAsync(invalidDto));
    }

    [Fact]
    public async Task CreateEvent_StartInPast_ThrowsValidationException()
    {
        // Arrange
        var invalidDto = new EventDto
        {
            Title = "Past Event",
            StartAt = DateTime.UtcNow.AddDays(-1), // прошлый день
            EndAt = DateTime.UtcNow,
            TotalSeats = 10
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.AddEventAsync(invalidDto));
    }

    [Fact]
    public async Task CreateEvent_StartAfterEnd_ThrowsValidationException()
    {
        // Arrange
        var invalidDto = new EventDto
        {
            Title = "Bad Schedule",
            StartAt = DateTime.UtcNow.AddDays(2),
            EndAt = DateTime.UtcNow.AddDays(1), // конец раньше начала
            TotalSeats = 10
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.AddEventAsync(invalidDto));
    }

    // ========================================================================
    // 2. Получение всех событий
    // ========================================================================

    [Fact]
    public async Task GetEventsAsync_WithThreeAddedEvents_ReturnsCorrectlyMappedDtoList()
    {
        // Arrange
        var eventsToAdd = ValidEventDtos()
            .Select(data => (EventDto)data[0])
            .ToList();

        // Подготавливаем реальные события (как будто они сохранены в БД)
        var savedEvents = new List<Event>();
        foreach (var dto in eventsToAdd)
        {
            var @event = Event.Create(
                dto.Title!.Trim(),
                (DateTime)dto.StartAt!,
                (DateTime)dto.EndAt!,
                dto.TotalSeats,
                dto.Description ?? string.Empty);

            savedEvents.Add(@event);
        }

        // Мокаем AddAsync
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Мокаем GetFilteredPagesAsync — он вернёт подготовленные события
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                null, // title
                null, // from
                null, // to
                1,    // page
                10,   // pageSize
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = savedEvents.Count,
                Page = 1,
                PageSize = 10,
                Items = savedEvents
            });

        // Мокаем SaveChangesAsync
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act — добавляем события (только мокаем AddAsync)
        foreach (var dto in eventsToAdd)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Теперь вызываем GetEventsAsync
        var result = await _eventService.GetEventsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);

        // Проверяем маппинг: каждый элемент должен быть EventDtoResponse
        foreach (var item in result.Items)
        {
            item.Id.Should().NotBe(Guid.Empty);
            item.Title.Should().NotBeNullOrEmpty();
            item.StartAt.Should().BeBefore((DateTime)item.EndAt!);
        }

        // Проверяем вызовы
        _eventRepoMock.Verify(r => r.GetFilteredPagesAsync(
            null, null, null, 1, 10, It.IsAny<CancellationToken>()), Times.Once);

        _eventRepoMock.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }




    // ========================================================================
    // 3. Получение события по ID
    // ========================================================================
    // Проверяет, что существующее событие возвращается корректно

    [Fact]
    public async Task GetEventById_ExistingId_ReturnsEvent()
    {
        // Arrange
        var testEventDto = ValidEventDtos().First();
        var inputEvent = (EventDto)testEventDto[0];

        // Подготавливаем ожидаемое событие
        var expectedEvent = Event.Create(
            inputEvent.Title!.Trim(),
            (DateTime)inputEvent.StartAt!,
            (DateTime)inputEvent.EndAt!,
            inputEvent.TotalSeats,
            inputEvent.Description ?? string.Empty);

        // Мокаем GetEventAsync → он вызывает GetByIdAsync
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEvent);

        // Мокаем AddAsync (для "добавления")
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var addedEvent = await _eventService.AddEventAsync(inputEvent);
        var result = await _eventService.GetEventAsync(addedEvent.Id);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(inputEvent.Title);
        result.Description.Should().Be(inputEvent.Description ?? string.Empty);
        result.StartAt.Should().Be(inputEvent.StartAt);
        result.EndAt.Should().Be(inputEvent.EndAt);

        // Проверяем вызовы
        _eventRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventRepoMock.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    // Проверяет, что при отсутствии события выбрасывается NotFoundException
    [Fact]
    public async Task GetEvent_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>
                (async () => await _eventService.GetEventAsync(nonExistingId));


    Assert.Contains($"Событие с ID {nonExistingId} не найдено", exception.Message);
    }

    // ========================================================================
    // 4. Обновление существующего события
    // ========================================================================
    // Проверяет, что событие обновляется с новыми валидными данными

    [Fact]
    public async Task UpdateEvent_ExistingIdAndValidData_UpdatesAndReturnsEvent()
    {
        // Arrange
        var originalDto = (EventDto)ValidEventDtos().First()[0]!;

        // 1. Создаём событие через AddEventAsync
        var addedEvent = await _eventService.AddEventAsync(originalDto);

        // 2. Готовим данные для обновления
        var updateDto = new EventDto
        {
            Title = "Обновлённое название",
            Description = "Новое описание",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 10
        };

        // 3. Подготавливаем "изначальное" событие (такое, которое вернётся из GetByIdAsync)
        var originalEvent = Event.Create(
            originalDto.Title!.Trim(),
            (DateTime)originalDto.StartAt!,
            (DateTime)originalDto.EndAt!,
            originalDto.TotalSeats,
            originalDto.Description ?? string.Empty);
        originalEvent.Id = addedEvent.Id;

        // 4. Мокаем GetByIdAsync
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(addedEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalEvent);

        // 5. Мокаем SaveChangesAsync — он сохранит изменения, но в Moq мы не можем проверить изменения напрямую
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _eventService.UpdateEventAsync(addedEvent.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(addedEvent.Id); // ← теперь совпадёт, если GetByIdAsync вернул originalEvent с тем же ID
        result.Title.Should().Be(updateDto.Title);
        result.Description.Should().Be(updateDto.Description);
        result.StartAt.Should().Be(updateDto.StartAt);
        result.EndAt.Should().Be(updateDto.EndAt);
        result.TotalSeats.Should().Be(updateDto.TotalSeats);

        // Проверяем вызовы
        _eventRepoMock.Verify(r => r.GetByIdAsync(addedEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEvent_EmptyTitle_ThrowsValidationException()
    {
        var added = await _eventService.AddEventAsync((EventDto)ValidEventDtos().First()[0]!);
        var updateDto = new EventDto { Title = string.Empty };

        // Создаём событие через Event.Create — он вернёт Event с новым ID
        var originalEvent = Event.Create(
            title: "Old Title",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Old Description");

        // Мокаем GetByIdAsync — вернёт originalEvent, но при любом ID (It.IsAny<Guid>())
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalEvent);

        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.UpdateEventAsync(added.Id, updateDto));
    }


    [Fact]
    public async Task UpdateEvent_StartAfterEnd_ThrowsValidationException()
    {
        // Arrange
        var added = await _eventService.AddEventAsync((EventDto)ValidEventDtos().First()[0]!);
        var updateDto = new EventDto
        {
            Title = "Тест",
            StartAt = DateTime.UtcNow.AddDays(2), // позже, чем EndAt
            EndAt = DateTime.UtcNow.AddDays(1),   // раньше — нарушение!
            TotalSeats = 10
        };

        // Создаём "изначальное" событие через Event.Create — вернёт Event с любым ID
        var originalEvent = Event.Create(
            title: "Old Title",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Old Description");

        // Мокаем GetByIdAsync — вернёт originalEvent при любом ID
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalEvent);

        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert — валидация `StartAt < EndAt` сработает внутри Event.ThrowIfNotValid(...)
        await Assert.ThrowsAsync<ValidationException>(() =>
            _eventService.UpdateEventAsync(added.Id, updateDto));
    }

    // Проверяет: обновление несуществующего ID → ошибка
    [Fact]
    public async Task UpdateEvent_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var updateDto = new EventDto { Title = "Тест", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10 };
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(async () => await _eventService.UpdateEventAsync(nonExistingId, updateDto));
        Assert.Contains("не найдено", ex.Message);
    }

    // ========================================================================
    // 5. Удаление события
    // ========================================================================
    // Проверяет, что событие удаляется и больше не доступно

    [Fact]
    public async Task DeleteEvent_ExistingId_DeletesEvent()
    {
        // Arrange
        var addedEvent = await _eventService.AddEventAsync((EventDto)ValidEventDtos().First()[0]!);
        var id = addedEvent.Id;

        // Подготавливаем "существующее" событие для RemoveEventAsync
        var @event = Event.Create(
            title: "Old Title",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Old Description");

        // Мокаем GetByIdAsync (для RemoveEventAsync)
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Мокаем DeleteAsync (если он есть)
        _eventRepoMock
            .Setup(r => r.DeleteAsync(@event, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Мокаем SaveChangesAsync
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: удаляем событие
        await _eventService.RemoveEventAsync(id);

        // Теперь мокаем GetByIdAsync для GetEventAsync — возвращаем null
        _eventRepoMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act & Assert: пытаемся получить удалённое событие — должно бросить NotFoundException
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _eventService.GetEventAsync(id));

        Assert.Contains($"Событие с ID {id} не найдено", exception.Message);
    }

    // Проверяет: удаление несуществующего ID → ошибка
    [Fact]
    public async Task RemoveEvent_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>
                (async () => await _eventService.RemoveEventAsync(nonExistingId)
        );

        Assert.Contains("не найдено", exception.Message);
    }

    // ========================================================================
    // 6. Фильтрация по названию
    // ========================================================================
    // Проверяет частичное, регистронезависимое совпадение по заголовку


    [Fact]
    public async Task GetEvents_TitleFilter_MatchesPartialAndCaseInsensitive()
    {
        // Arrange
        var testEvents = ValidEventDtos()
            .Select(evt => (EventDto)evt[0]!)
            .Where(dto => dto.Title?.Contains("план", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();


        // Подготавливаем события, которые будут "возвращены" репозиторием
        var savedEvents = new List<Event>();
        foreach (var dto in testEvents)
        {
            var @event = Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty);
            savedEvents.Add(@event);
        }

        // Мокаем AddEventAsync (просто возвращаем Task, не храним данные)
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Мокаем GetFilteredPagesAsync — он будет вызван внутри GetEventsAsync(title: "план")
        // Он принимает title, from, to, page, pageSize, ct
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                "план", // title — точное значение из вызова
                null, null, // from, to
                1, 10, // page, pageSize
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = savedEvents.Count,
                Page = 1,
                PageSize = 10,
                Items = savedEvents
            });

        // Мокаем SaveChangesAsync (не нужен здесь, но на всякий случай)
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события (только мокаем AddAsync)
        foreach (var dto in testEvents)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Теперь вызываем GetEventsAsync с фильтром
        var result = await _eventService.GetEventsAsync(title: "план");

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(savedEvents.Count);
        result.Items.Should().HaveCount(savedEvents.Count);

        // Проверяем, что в каждом результате есть "план" (регистронезависимо)
        Assert.All(result.Items, item =>
            Assert.Contains("план", item.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData("", 3)]
    [InlineData("   ", 3)]
    [InlineData("несуществующее событие", 0)]
    public async Task GetEvents_TitleFilter_NullOrEmpty_ReturnsAllEvents(string? title, int expectedCount)
    {
        // Arrange: получаем ВСЕ события (null — значит без фильтра)
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        //var expectedCount = allEventsDto.Count;

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        // Мокаем AddEventAsync
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Ловим любое значение title — так как GetEventsAsync сам обрабатывает null/пустые/пробелы
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(),
                null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = expectedCount,
                Items = expectedCount > 0 ? savedEvents : new List<Event>()
            });

        // Мокаем SaveChangesAsync
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события (только мокаем AddAsync)
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync без фильтра)
        var result = await _eventService.GetEventsAsync(title: title);

        // Assert
        result.TotalCount.Should().Be(expectedCount);
        result.Items.Should().HaveCount(expectedCount);
    }

    // ========================================================================
    // 7. Фильтрация по датам
    // ========================================================================
    // Проверяет фильтр "от" — события, начинающиеся не раньше указанной даты

    [Fact]
    public async Task GetEvents_FromFilter_ReturnsEventsStartingAfterOrAt()
    {
        // Arrange
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();

        // Вычисляем ожидаемое количество событий (начинающихся не раньше `from`)
        var from = DateTime.UtcNow.AddDays(1);
        var expectedEventsDto = allEventsDto.Where(e => e.StartAt >= from).ToList();
        var expectedCount = expectedEventsDto.Count;

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        // ✅ Мокаем GetFilteredPagesAsync с параметром from
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                from,               // from — ТОЧНО это значение
                null,              // to — не фильтруем
                1, 10,             // page, pageSize
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = expectedCount,
                Items = savedEvents.Where(e => e.StartAt >= from).ToList() // ← фильтрация в репозитории
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события (только мокаем AddAsync)
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с фильтром from
        var result = await _eventService.GetEventsAsync(from: from);

        // Assert
        result.TotalCount.Should().Be(expectedCount);
        result.Items.Should().HaveCount(expectedCount);
        result.Items.Should().OnlyContain(item => item.StartAt >= from);
    }


    [Fact]
    public async Task GetEvents_ToFilter_ReturnsEventsEndingBeforeOrAt()
    {
        // Arrange
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();

        var to = new DateTime(2026, 5, 15, 23, 59, 59, DateTimeKind.Utc);
        var expectedEventsDto = allEventsDto.Where(e => e.EndAt <= to).ToList();
        var expectedCount = expectedEventsDto.Count;

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        // Мокаем GetFilteredPagesAsync с параметром to
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                null,              // from — не фильтруем
                to,                // to — ТОЧНО это значение
                1, 10,             // page, pageSize
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = expectedCount,
                Items = savedEvents.Where(e => e.EndAt <= to).ToList()
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с фильтром to
        var result = await _eventService.GetEventsAsync(to: to);

        // Assert
        result.TotalCount.Should().Be(expectedCount);
        result.Items.Should().HaveCount(expectedCount);
        result.Items.Should().OnlyContain(item => item.EndAt <= to);
    }


    [Fact]
    public async Task GetEvents_FromAndToFilter_ReturnsEventsInDateRange()
    {
        // Arrange
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();

        var from = DateTime.UtcNow.AddDays(1);
        var to = DateTime.UtcNow.AddDays(2);

        var expectedEventsDto = allEventsDto
            .Where(e => e.StartAt >= from && e.EndAt <= to)
            .ToList();
        var expectedCount = expectedEventsDto.Count;

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        // Мокаем GetFilteredPagesAsync с обеими датами: from и to
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                from,               // from — ТОЧНО это значение
                to,                 // to — ТОЧНО это значение
                1, 10,              // page, pageSize
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = expectedCount,
                Items = savedEvents
                    .Where(e => e.StartAt >= from && e.EndAt <= to)
                    .ToList()
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с from + to
        var result = await _eventService.GetEventsAsync(from: from, to: to);

        // Assert
        result.TotalCount.Should().Be(expectedCount);
        result.Items.Should().HaveCount(expectedCount);

        result.Items.Should().OnlyContain(item => item.StartAt >= from && item.EndAt <= to);
    }

    // Проверяет: from в будущем → пусто
    [Fact]
    public async Task GetEvents_FromFilter_FutureDate_ReturnsEmptyList()
    {
        // Arrange
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();

        var from = DateTime.UtcNow.AddYears(10); // далеко в будущем

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        // Мокаем GetFilteredPagesAsync — возвращает пустой список для from в будущем
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                from,               // from — будущая дата
                null,              // to — без фильтра
                1, 10,             // page, pageSize
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = 0,
                Items = new List<Event>() // ← пустой список
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с будущей датой from
        var result = await _eventService.GetEventsAsync(from: from);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // ========================================================================
    // 8. Пагинация
    // ========================================================================
    // Проверяет вторую страницу при pageSize=2

    [Fact]
    public async Task GetEvents_Pagination_Page2_Size2_ReturnsCorrectSubset()
    {
        // Arrange
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        var totalEventsCount = allEventsDto.Count; // ожидаем, что их 3

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        const int page = 2;
        const int pageSize = 2;

        // Для пагинации: GetFilteredPagesAsync(page: 2, pageSize: 2) → возвращает 2-ю страницу (индекс 1)
        //Items = savedEvents.Skip((page - 1) * pageSize).Take(pageSize).ToList(); // → 1 элемент (индекс 2)

        // Мокаем GetFilteredPagesAsync — он делает пагинацию
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                null, null,         // from, to
                page, pageSize,     // page = 2, pageSize = 2
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = totalEventsCount,
                Page = page,
                PageSize = pageSize,
                Items = savedEvents.Skip((page - 1) * pageSize).Take(pageSize).ToList() // → только 1 элемент
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с пагинацией
        var result = await _eventService.GetEventsAsync(page: page, pageSize: pageSize);

        // Assert
        result.TotalCount.Should().Be(totalEventsCount);
        result.PageSize.Should().Be(pageSize);
        result.Page.Should().Be(page);
        result.Items.Should().HaveCount(1); // На 2-й странице — 1 событие (если всего 3)
    }


    // Проверяет первую страницу
    [Fact]
    public async Task GetEvents_Pagination_Page1_Size2_ReturnsFirstTwoItems()
    {
        // Arrange
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        var totalEventsCount = allEventsDto.Count; // ожидаем, что их 3

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        const int page = 1;
        const int pageSize = 2;

        // Мокаем GetFilteredPagesAsync — page = 1, pageSize = 2
        // Возвращает первые 2 события
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                null, null,         // from, to
                page, pageSize,     // page = 1, pageSize = 2
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = totalEventsCount,
                Page = page,
                PageSize = pageSize,
                Items = savedEvents.Skip((page - 1) * pageSize).Take(pageSize).ToList() // → 2 элемента
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с page = 1, pageSize = 2
        var result = await _eventService.GetEventsAsync(page: page, pageSize: pageSize);

        // Assert
        result.TotalCount.Should().Be(totalEventsCount);
        result.PageSize.Should().Be(pageSize);
        result.Page.Should().Be(page);
        result.Items.Should().HaveCount(2);
    }

    // Проверяет нормализацию: page=0 → page=1
    [Fact]
    public async Task GetEvents_Pagination_Page0_ReturnsFirstPage()
    {
        // Arrange
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        var totalEventsCount = allEventsDto.Count;

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        const int page = 0;
        const int pageSize = 2;

        // Мокаем GetFilteredPagesAsync — он получает page=1 (если GetEventsAsync нормализует page=0 → 1)
        // Или мокаем page=0 — зависит от того, делает ли GetEventsAsync нормализацию
        // Скорее всего: GetEventsAsync(page=0) → вызывает GetFilteredPagesAsync(page=1, ...)

        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                null, null,         // from, to
                1, pageSize,        // page = 1 (после нормализации)
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = totalEventsCount,
                Page = 1,
                PageSize = pageSize,
                Items = savedEvents.Take(pageSize).ToList()
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с page=0 (ожидаем нормализацию в page=1)
        var result = await _eventService.GetEventsAsync(page: page, pageSize: pageSize);

        // Assert
        result.Page.Should().Be(1);       // ← нормализовано
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().Be(totalEventsCount);
        result.Items.Should().HaveCount(2);
    }

    // Проверяет нормализацию: pageSize=0 → pageSize=1
    [Fact]
    public async Task GetEvents_PageSize0_NormalizesTo1()
    {
        // Arrange
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();
        var totalEventsCount = allEventsDto.Count;

        // Подготавливаем реальные события
        var savedEvents = allEventsDto
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        const int page = 1;
        const int pageSize = 0;

        // Мокаем GetFilteredPagesAsync — он получает pageSize=1 (если GetEventsAsync нормализует pageSize=0 → 1)
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                null, null,         // from, to
                page, 1,            // pageSize = 1 (нормализовано из 0)
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = totalEventsCount,
                Page = page,
                PageSize = 1, // ← нормализовано
                Items = savedEvents.Take(1).ToList()
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем события
        foreach (var dto in allEventsDto)
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с pageSize=0 (ожидаем нормализацию в 1)
        var result = await _eventService.GetEventsAsync(page: page, pageSize: pageSize);

        // Assert
        result.PageSize.Should().Be(1);         // ← нормализовано
        result.Items.Should().HaveCount(1);     // Single
    }

    // Проверяет ограничение: pageSize=150 → pageSize=100
    [Fact]
    public async Task GetEvents_PageSize150_NormalizesTo100()
    {
        // Arrange: добавляем 50 событий
        for (int i = 0; i < 50; i++)
        {
            await _eventService.AddEventAsync(new EventDto
            {
                Title = $"Событие {i}",
                StartAt = DateTime.UtcNow.AddDays(i + 1),
                EndAt = DateTime.UtcNow.AddDays(i + 2),
                TotalSeats = 10
            });
        }

        const int inputPageSize = 150;
        const int expectedPageSize = 100; // максимум

        // Подготавливаем реальные события (50 штук)
        var savedEvents = Enumerable.Range(0, 50)
            .Select(i => Event.Create(
                title: $"Событие {i}",
                startAt: DateTime.UtcNow.AddDays(i + 1),
                endAt: DateTime.UtcNow.AddDays(i + 2),
                totalSeats: 10,
                description: string.Empty))
            .ToList();

        var totalEventsCount = savedEvents.Count;

        // Мокаем GetFilteredPagesAsync — pageSize=100 (нормализовано из 150)
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                It.IsAny<string>(), // title
                null, null,         // from, to
                1, expectedPageSize, // pageSize = 100 (нормализовано)
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = totalEventsCount,
                Page = 1,
                PageSize = expectedPageSize,
                Items = savedEvents.Take(expectedPageSize).ToList() // → 50, т.к. всего 50
            });

        // Мокаем SaveChangesAsync
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: вызываем GetEventsAsync с pageSize=150 (ожидаем нормализацию в 100)
        var result = await _eventService.GetEventsAsync(page: 1, pageSize: inputPageSize);

        // Assert
        result.PageSize.Should().Be(expectedPageSize); // 100
        result.Items.Should().HaveCount(50);           // всего 50 событий
        result.Items.Count.Should().BeLessThanOrEqualTo(100); // Optional, но добавляет ясности
    }

    // ========================================================================
    // 9. Комбинированная фильтрация (логическое И)
    // ========================================================================
    // Проверяет одновременное применение фильтров: title + from + to

    [Fact]
    public async Task GetEvents_CombinedFilters_ReturnsIntersection()
    {
        // Arrange: получаем все события и фильтруем их, чтобы оставить только нужные
        var allEventsDto = ValidEventDtos().Select(evt => (EventDto)evt[0]!).ToList();

        var from = DateTime.UtcNow.AddHours(23);
        var to = DateTime.UtcNow.AddDays(2);
        var title = "встреча";

        // Ожидаемое событие: должно соответствовать ВСЕМ трём критериям
        var expectedEventsDto = allEventsDto
            .Where(e =>
                e.Title?.Contains(title, StringComparison.OrdinalIgnoreCase) == true &&
                e.StartAt >= from &&
                e.EndAt <= to)
            .ToList();

        var expectedCount = expectedEventsDto.Count;

        // Убедимся, что мы ожидаем ровно 1 событие (для Assert.Single)
        if (expectedCount == 0)
        {
            // Если в ValidEventDtos нет подходящих — просто создадим одно вручную
            expectedEventsDto.Add(new EventDto
            {
                Title = "Встреча с командой (встреча)",
                StartAt = DateTime.UtcNow.AddHours(24),
                EndAt = DateTime.UtcNow.AddHours(26),
                TotalSeats = 10,
                Description = "Обсуждение планов"
            });
            expectedCount = 1;
        }

        // Подготавливаем реальные события (все, не только ожидаемые)
        var savedEvents = allEventsDto
            .Concat(expectedEventsDto.Where(e => !allEventsDto.Contains(e))) // добавляем, если не было
            .Select(dto => Event.Create(
                title: dto.Title!.Trim(),
                startAt: (DateTime)dto.StartAt!,
                endAt: (DateTime)dto.EndAt!,
                totalSeats: dto.TotalSeats,
                description: dto.Description ?? string.Empty))
            .ToList();

        // Мокаем GetFilteredPagesAsync — он должен вернуть только пересечение
        _eventRepoMock
            .Setup(r => r.GetFilteredPagesAsync(
                title,              // title = "встреча"
                from,               // from = ... (точно)
                to,                 // to = ... (точно)
                1, 10,              // page, pageSize
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                TotalCount = expectedCount,
                Items = savedEvents
                    .Where(e =>
                        e.Title?.Contains(title, StringComparison.OrdinalIgnoreCase) == true &&
                        e.StartAt >= from &&
                        e.EndAt <= to)
                    .ToList()
            });

        // Мокаем AddAsync и SaveChanges
        _eventRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _eventRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act: добавляем все события
        foreach (var dto in allEventsDto.Concat(expectedEventsDto.Where(e => !allEventsDto.Contains(e))))
        {
            await _eventService.AddEventAsync(dto);
        }

        // Act: вызываем GetEventsAsync с тремя фильтрами
        var result = await _eventService.GetEventsAsync(title: title, from: from, to: to);

        // Assert
        result.TotalCount.Should().Be(expectedCount);
        result.Items.Should().HaveCount(expectedCount);

        if (expectedCount > 0)
        {
            var evt = result.Items.First();
            Assert.Contains(title, evt.Title, StringComparison.OrdinalIgnoreCase);
            Assert.True(evt.StartAt >= from, "Событие должно начинаться не раньше 'from'");
            Assert.True(evt.EndAt <= to, "Событие должно заканчиваться не позже 'to'");
        }
    }
}