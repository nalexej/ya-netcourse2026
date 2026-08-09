using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Application.Interfaces;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Реализация сервиса управления событиями.
/// Предоставляет бизнес-логику для операций: получение, добавление, обновление, удаление событий.
/// Все методы — асинхронные.
/// </summary>
public sealed class EventService : IEventService
{
    private readonly IEventRepository _repository;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EventService"/>.
    /// </summary>
    /// <param name="repository">Репозиторий для доступа к данным. Не должен быть <see langword="null"/>.</param>
    public EventService(IEventRepository repository)
    {
        _repository = repository;
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
        var eventEntity = await _repository.GetByIdAsync(id, cancellationToken) 
                    ?? throw new NotFoundException($"Событие с ID {id} не найдено.");
        return eventEntity.ToDtoResponse();
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

        return eventEntity.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<bool> RemoveEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _repository.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException($"Событие с ID {id} не найдено."); ;

        await _repository.DeleteAsync(eventEntity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}