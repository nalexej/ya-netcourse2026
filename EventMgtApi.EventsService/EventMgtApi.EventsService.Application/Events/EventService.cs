using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Contracts.Caching;
using EventMgtApi.Contracts.Events.DTOs;
using EventMgtApi.Contracts.Options;
using EventMgtApi.EventsService.Application.Caching;
using EventMgtApi.EventsService.Application.Extensions;
using EventMgtApi.EventsService.Application.Persistence;
using EventMgtApi.EventsService.Domain.Entities;
using EventMgtApi.EventsService.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace EventMgtApi.EventsService.Application.Services;

/// <summary>
/// Реализация сервиса управления событиями.
/// Предоставляет бизнес-логику для операций: получение, добавление, обновление, удаление событий.
/// Все методы — асинхронные.
/// </summary>
public sealed class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly ICacheClient _cache;
    private readonly EventCacheOptions _eventCacheOptions;


    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EventService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий для доступа к данным. Не должен быть <see langword="null"/>.</param>
    /// <param name="cache">Кэш. Не должен быть <see langword="null"/>.</param>
    /// <param name="eventCacheOptions">Настройки кэша событий из конфигурации.</param>
    public EventService(IEventRepository repository, ICacheClient cache, IOptions<EventCacheOptions> eventCacheOptions)
    {
        _repository = repository;
        _cache = cache;
        _eventCacheOptions = eventCacheOptions.Value;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<EventDtoResponse>> GetEventsAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Защита от некорректных значений
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Ограничим максимум 100

        var result = await _repository.GetFilteredPagesAsync(title, from, to, page, pageSize, cancellationToken);

        return new PaginatedResult<EventDtoResponse>
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Items = result.Items.ToDtoList() // Маппинг в DTO
        };
    }

    /// <inheritdoc />
    public async Task<EventDtoResponse> GetEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = EventCacheKeys.ForEvent(id);

        // 1. Попытка прочитать из кэша
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached != null)
        {
            return System.Text.Json.JsonSerializer.Deserialize<EventDtoResponse>(cached)!;
        }

        // 2. Кэш промах — читаем из БД
        var eventEntity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Событие с ID {id} не найдено.");

        var dto = eventEntity.ToDtoResponse();

        // 3. Сохраняем в кэш
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        await _cache.SetStringAsync(EventCacheKeys.ForEvent(id), json, TimeSpan.FromSeconds(_eventCacheOptions.EventTtlSeconds), cancellationToken);

        return dto;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TopEventDto>> GetTopEventsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var cacheKey = EventCacheKeys.TopEvents;

        // 1. Читаем из кэша
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached != null)
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<IEnumerable<TopEventDto>>(cached)!;
            return result;
        }

        // 2. Кэш промах — читаем из БД
        var topEvents = await _repository.GetTopEventsAsync(count, cancellationToken);
        var list = topEvents.ToList();

        // 3. Сохраняем в кэш
        var json = System.Text.Json.JsonSerializer.Serialize(list);
        await _cache.SetStringAsync(cacheKey, json, TimeSpan.FromSeconds(_eventCacheOptions.TopEventsTtlSeconds), cancellationToken);

        return list;
    }

    /// <inheritdoc />
    public async Task<EventDtoResponse> AddEventAsync(EventDto evtDto, CancellationToken cancellationToken = default)
    {
        // Дополнительная защита: на случай, если метод вызван без валидации модели
        ArgumentNullException.ThrowIfNull(evtDto, nameof(evtDto));

        // Используем фабричный метод Event.Create с валидацией
        var eventEntity = Event.Create(
            title: evtDto.Title,
            startAt: evtDto.StartAt ?? throw new ValidationException("Дата начала обязательна."),
            endAt: evtDto.EndAt ?? throw new ValidationException("Дата окончания обязательна."),
            totalSeats: evtDto.TotalSeats,
            description: evtDto.Description
        );

        await _repository.AddAsync(eventEntity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return eventEntity.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<EventDtoResponse> UpdateEventAsync(Guid id, EventDto evtDto, CancellationToken cancellationToken = default)
    {
        // Дополнительная защита: на случай, если метод вызван без валидации модели
        ArgumentNullException.ThrowIfNull(evtDto, nameof(evtDto));

        var eventEntity = await _repository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException($"Событие с ID {id} не найдено."); ;

        eventEntity.Update(evtDto.Title, evtDto.StartAt, evtDto.EndAt, evtDto.Description);
        await _repository.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(EventCacheKeys.ForEvent(id), cancellationToken);
        return eventEntity.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<bool> RemoveEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _repository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException($"Событие с ID {id} не найдено."); ;

        await _repository.DeleteAsync(eventEntity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(EventCacheKeys.ForEvent(eventEntity.Id), cancellationToken);
        return true;
    }
}