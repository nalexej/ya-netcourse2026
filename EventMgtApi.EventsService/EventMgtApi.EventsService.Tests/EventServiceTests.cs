using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.EventsService.Application.Caching;
using EventMgtApi.Contracts.Events.DTOs;
using EventMgtApi.EventsService.Application.Persistence;
using EventMgtApi.EventsService.Application.Services;
using EventMgtApi.EventsService.Domain.Entities;
using EventMgtApi.EventsService.Domain.Exceptions;
using Microsoft.Extensions.Options;
using Moq;

namespace EventMgtApi.EventsService.Tests;

/// <summary>
/// Unit-тесты для <see cref="EventService"/> с моками <see cref="IEventRepository"/> и <see cref="ICacheClient"/>.
/// Проверяют паттерн cache-aside: попадание/промах кеша и инвалидацию при мутациях.
/// </summary>
public class EventServiceTests
{
    #region Helpers

    /// <summary>Универсальный Id для тестовых сценариев.</summary>
    private static readonly Guid TestEventId = Guid.NewGuid();

    /// <summary>Создаёт доменную сущность Event с заданными параметрами.</summary>
    private static Event CreateEntity(Guid? id = null, string? title = "Concert", string? description = "Test desc")
    {
        var entity = Event.Create(
            title: title!,
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            totalSeats: 100,
            description: description
        );
        if (id.HasValue)
            entity.Id = id.Value;
        return entity;
    }

    /// <summary>Создаёт валидный <see cref="EventDto"/> для ввода.</summary>
    private static EventDto CreateDto(Guid? id = null) =>
        new()
        {
            Title = "Concert",
            Description = "Test desc",
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2),
            TotalSeats = 100
        };

    /// <summary>Создаёт ожидаемый <see cref="EventDtoResponse"/> для сравнения с результатом.</summary>
    private static EventDtoResponse CreateDtoResponse(Guid? id = null) =>
        new()
        {
            Id = id ?? TestEventId,
            Title = "Concert",
            Description = "Test desc",
            StartAt = DateTime.UtcNow.AddHours(1),
            EndAt = DateTime.UtcNow.AddHours(2),
            TotalSeats = 100,
            AvailableSeats = 100
        };

    /// <summary>
    /// Создаёт System Under Test (SUT) — <see cref="EventService"/> с настраиваемыми моками.
    /// Позволяет задать ответы для GetById, GetFilteredPagesAsync и GetTopEventsAsync.
    /// </summary>
    private (Mock<IEventRepository> repository, Mock<ICacheClient> cache, EventService service) CreateSut(
        Event? singleEvent = null,
        IEnumerable<Event>? allEvents = null,
        IEnumerable<TopEventDto>? topEvents = null)
    {
        var repository = new Mock<IEventRepository>();
        var cache = new Mock<ICacheClient>();
        var cacheOptions = Options.Create(new EventCacheOptions { EventTtlSeconds = 300, TopEventsTtlSeconds = 300 });

        // Default: GetById returns the single event
        if (singleEvent != null)
        {
            repository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(singleEvent);
        }

        if (allEvents != null)
        {
            repository
                .Setup(r => r.GetFilteredPagesAsync(It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaginatedResult<Event>
                {
                    TotalCount = allEvents.Count(),
                    Page = 1,
                    PageSize = 10,
                    Items = allEvents.ToList()
                });
        }

        if (topEvents != null)
        {
            repository
                .Setup(r => r.GetTopEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(topEvents);
        }

        var service = new EventService(repository.Object, cache.Object, cacheOptions);
        return (repository, cache, service);
    }

    #endregion

    #region GetEventAsync — Cache Hit / Cache Miss

    /// <summary>
    /// Кеш содержит запись: сервис возвращает десериализованный DTO,
    /// не вызывает репозиторий и не записывает в кеш.
    /// </summary>
    [Fact]
    public async Task GetEventAsync_CacheHit_ReturnsFromCache_RepositoryNotCalled()
    {
        // Arrange
        var response = CreateDtoResponse();
        var json = System.Text.Json.JsonSerializer.Serialize(response);
        var cacheKey = EventCacheKeys.ForEvent(TestEventId);

        var (_, cache, service) = CreateSut();

        cache
            .Setup(c => c.GetStringAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        // Act
        var result = await service.GetEventAsync(TestEventId);

        // Assert
        Assert.Equal(response.Id, result.Id);
        Assert.Equal(response.Title, result.Title);
        // Cache write MUST NOT happen on cache hit
        cache.Verify(c => c.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Кеш пуст: сервис читает из репозитория, сериализует результат и сохраняет в кеш.
    /// </summary>
    [Fact]
    public async Task GetEventAsync_CacheMiss_RepositoryCalled_AndResultCached()
    {
        // Arrange
        var entity = CreateEntity(TestEventId);
        var response = CreateDtoResponse(TestEventId);
        var cacheKey = EventCacheKeys.ForEvent(TestEventId);

        var (repository, cache, service) = CreateSut(singleEvent: entity);

        // Cache returns null (miss)
        cache
            .Setup(c => c.GetStringAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await service.GetEventAsync(TestEventId);

        // Assert
        Assert.Equal(response.Id, result.Id);
        Assert.Equal(response.Title, result.Title);

        // Repository was called
        repository.Verify(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>()), Times.Once);

        // Result was written to cache
        cache.Verify(
            c => c.SetStringAsync(
                cacheKey,
                It.Is<string>(s => s.Contains(TestEventId.ToString())),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Кеш пуст и репозиторий не нашёл сущность: выбрасывается NotFoundException.
    /// </summary>
    [Fact]
    public async Task GetEventAsync_CacheMiss_EntityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repository = new Mock<IEventRepository>();
        var cache = new Mock<ICacheClient>();
        var cacheOptions = Options.Create(new EventCacheOptions { EventTtlSeconds = 300 });
        var service = new EventService(repository.Object, cache.Object, cacheOptions);

        var cacheKey = EventCacheKeys.ForEvent(TestEventId);
        cache.Setup(c => c.GetStringAsync(cacheKey, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        repository.Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetEventAsync(TestEventId));
        Assert.Contains(TestEventId.ToString(), ex.Message);
    }

    #endregion

    #region GetTopEventsAsync — Cache Hit / Cache Miss

    /// <summary>
    /// Кеш содержит запись: сервис возвращает топовые события без обращения к репозиторию.
    /// </summary>
    [Fact]
    public async Task GetTopEventsAsync_CacheHit_ReturnsFromCache_RepositoryNotCalled()
    {
        // Arrange
        var topEvents = new List<TopEventDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Top1", TotalSeats = 500, AvailableSeats = 100, SoldPercent = 80m }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(topEvents);

        var (repository, cache, service) = CreateSut();

        cache
            .Setup(c => c.GetStringAsync(EventCacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync(json);

        // Act
        var result = await service.GetTopEventsAsync(10);

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal("Top1", list[0].Title);

        // Repository must NOT be called
        repository.Verify(r => r.GetTopEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Кеш пуст: сервис читает топ из репозитория и сохраняет результат в кеш.
    /// </summary>
    [Fact]
    public async Task GetTopEventsAsync_CacheMiss_RepositoryCalled_AndResultCached()
    {
        // Arrange
        var topEvents = new List<TopEventDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Top1", TotalSeats = 500, AvailableSeats = 100, SoldPercent = 80m },
            new() { Id = Guid.NewGuid(), Title = "Top2", TotalSeats = 300, AvailableSeats = 150, SoldPercent = 50m }
        };

        var (repository, cache, service) = CreateSut(topEvents: topEvents);

        cache
            .Setup(c => c.GetStringAsync(EventCacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await service.GetTopEventsAsync(10);

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);

        // Repository was called
        repository.Verify(r => r.GetTopEventsAsync(10, It.IsAny<CancellationToken>()), Times.Once);

        // Result was written to cache
        cache.Verify(
            c => c.SetStringAsync(
                EventCacheKeys.TopEvents,
                It.Is<string>(s => s.Contains("Top1")),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Кеш пуст и репозиторий вернул пустой список: в кеш всё равно сохраняется пустой массив.
    /// </summary>
    [Fact]
    public async Task GetTopEventsAsync_CacheMiss_EmptyResult_CacheSetWithEmptyList()
    {
        // Arrange
        var (repository, cache, service) = CreateSut(topEvents: Enumerable.Empty<TopEventDto>());

        cache
            .Setup(c => c.GetStringAsync(EventCacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await service.GetTopEventsAsync(5);

        // Assert
        Assert.Empty(result);

        // Cache was still written with empty list
        cache.Verify(
            c => c.SetStringAsync(
                EventCacheKeys.TopEvents,
                It.Is<string>(s => s.Contains("[]")),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Mutating Operations — Cache Invalidation

    /// <summary>
    /// Обновление события: после сохранения инвалидируется ключ единичного события.
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_CacheInvalidated()
    {
        // Arrange
        var entity = CreateEntity(TestEventId);
        var dto = new EventDto
        {
            Title = "Updated Concert",
            Description = "Updated desc",
            StartAt = DateTime.UtcNow.AddHours(3),
            EndAt = DateTime.UtcNow.AddHours(4),
            TotalSeats = 200
        };

        var (repository, cache, service) = CreateSut(singleEvent: entity);

        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await service.UpdateEventAsync(TestEventId, dto);

        // Assert
        Assert.Equal("Updated Concert", result.Title);
        Assert.Equal("Updated desc", result.Description);

        // Cache key for this event was removed
        cache.Verify(
            c => c.RemoveAsync(EventCacheKeys.ForEvent(TestEventId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Обновление несуществующего события: выбрасывается NotFoundException.
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_EntityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repository = new Mock<IEventRepository>();
        var cache = new Mock<ICacheClient>();
        var cacheOptions = Options.Create(new EventCacheOptions());
        var service = new EventService(repository.Object, cache.Object, cacheOptions);

        repository.Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateEventAsync(TestEventId, CreateDto()));
        Assert.Contains(TestEventId.ToString(), ex.Message);
    }

    /// <summary>
    /// Удаление события: после сохранения инвалидируется ключ единичного события
    /// и вызываются методы репозитория DeleteAsync + SaveChangesAsync.
    /// </summary>
    [Fact]
    public async Task RemoveEventAsync_CacheInvalidated()
    {
        // Arrange
        var entity = CreateEntity(TestEventId);
        var (repository, cache, service) = CreateSut(singleEvent: entity);

        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await service.RemoveEventAsync(TestEventId);

        // Assert
        Assert.True(result);

        // Cache key was removed
        cache.Verify(
            c => c.RemoveAsync(EventCacheKeys.ForEvent(TestEventId), It.IsAny<CancellationToken>()),
            Times.Once);

        // Repository delete was called
        repository.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Удаление несуществующего события: выбрасывается NotFoundException.
    /// </summary>
    [Fact]
    public async Task RemoveEventAsync_EntityNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repository = new Mock<IEventRepository>();
        var cache = new Mock<ICacheClient>();
        var cacheOptions = Options.Create(new EventCacheOptions());
        var service = new EventService(repository.Object, cache.Object, cacheOptions);

        repository.Setup(r => r.GetByIdAsync(TestEventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => service.RemoveEventAsync(TestEventId));
        Assert.Contains(TestEventId.ToString(), ex.Message);
    }

    /// <summary>
    /// Добавление события: кэш НЕ инвалидируется (текущее поведение сервиса).
    /// Вызываются AddAsync и SaveChangesAsync репозитория.
    /// </summary>
    [Fact]
    public async Task AddEventAsync_CacheNotInvalidated_RepositoryCalled()
    {
        // Arrange
        var dto = CreateDto();
        var (repository, cache, service) = CreateSut();

        // Act
        var result = await service.AddEventAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Title, result.Title);

        // Repository AddAsync was called
        repository.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Cache Remove should NOT be called for AddEventAsync (this is the current behavior)
        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetEventsAsync — No Caching

    /// <summary>
    /// Пагинированный список не использует кэш: данные всегда идут напрямую из репозитория.
    /// </summary>
    [Fact]
    public async Task GetEventsAsync_RepositoryCalled_CacheNotUsed()
    {
        // Arrange
        var entity1 = CreateEntity(Guid.NewGuid(), "Event 1");
        var entity2 = CreateEntity(Guid.NewGuid(), "Event 2");
        var entities = new List<Event> { entity1, entity2 };

        var (repository, cache, service) = CreateSut(allEvents: entities);

        // Act
        var result = await service.GetEventsAsync(title: "Event", page: 1, pageSize: 10);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Event 1", result.Items[0].Title);
        Assert.Equal("Event 2", result.Items[1].Title);

        // Cache should NOT be used for paginated list
        cache.Verify(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(c => c.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region AddEventAsync — Validation

    /// <summary>
    /// Передача null-DTO: выбрасывается ArgumentNullException.
    /// </summary>
    [Fact]
    public async Task AddEventAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        var (repository, cache, service) = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.AddEventAsync(null!));
    }

    /// <summary>
    /// Отсутствие обязательной даты начала: выбрасывается ValidationException.
    /// </summary>
    [Fact]
    public async Task AddEventAsync_NullStartAt_ThrowsValidationException()
    {
        // Arrange
        var dto = new EventDto { Title = "Event", TotalSeats = 10 };

        var (repository, cache, service) = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.AddEventAsync(dto));
        Assert.Contains("обязательна", ex.Message);
    }

    #endregion
}
