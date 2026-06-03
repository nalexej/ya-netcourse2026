using System.Threading.Tasks;
using EventMgtApi.Domain.Interfaces;
using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Entities;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Реализация сервиса управления событиями.
/// Предоставляет бизнес-логику для операций: получение, добавление, обновление, удаление событий.
/// Все методы — асинхронные.
/// </summary>
public class EventService : IEventService
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
        int pageSize = 10)
    {
        // Защита от некорректных значений
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize)); // Ограничим максимум 100

        // Получаем все события
        var allEvents = await _repository.GetAllAsync();

        // Применяем фильтры
        var query = allEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.EndAt <= to.Value);
        }

        // Подсчитываем общее количество
        var totalCount = query.Count();

        // Пагинация
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToDtoList();

        // Формируем результат
        return new PaginatedResult<EventDtoResponse>
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    /// <inheritdoc />
    public async Task<EventDtoResponse> GetEventAsync(Guid id)
    {
        var eventEntity = await _repository.GetByIdAsync(id);

        if (eventEntity is null)
            throw new NotFoundException($"Событие с ID {id} не найдено.");

        return eventEntity.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<EventDtoResponse> AddEventAsync(EventDto evtDto)
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

        await _repository.AddAsync(eventEntity);

        return eventEntity.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<EventDtoResponse> UpdateEventAsync(Guid id, EventDto evtDto)
    {
        // Дополнительная защита: на случай, если метод вызван без валидации модели
        ArgumentNullException.ThrowIfNull(evtDto, nameof(evtDto));

        // Используем фабричный метод Event.Create с валидацией
        var updatedEvent = Event.Create(
            title: evtDto.Title,
            startAt: evtDto.StartAt ?? throw new ValidationException("Дата начала обязательна."),
            endAt: evtDto.EndAt ?? throw new ValidationException("Дата окончания обязательна."),
            totalSeats: evtDto.TotalSeats,
            description: evtDto.Description
        );

        updatedEvent.Id = id;

        var success = await _repository.UpdateAsync(updatedEvent);

        if (!success)
            throw new NotFoundException($"Событие с ID {id} не найдено.");

        return updatedEvent.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task RemoveEventAsync(Guid id)
    {
        var success = await _repository.DeleteAsync(id);
        if (!success)
            throw new NotFoundException($"Событие с ID {id} не найдено.");
    }
}