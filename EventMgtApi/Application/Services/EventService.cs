using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Реализация сервиса управления событиями.
/// Предоставляет бизнес-логику для операций: получение, добавление, обновление, удаление событий.
/// Все методы — асинхронные.
/// </summary>
public sealed class EventService : IEventService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EventService"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных</param>
    public EventService(AppDbContext context)
    {
        _context = context;
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

        // Получаем все события
        var query = _context.Events.AsQueryable();

        // Применяем фильтры
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
        var totalCount = await query.CountAsync(cancellationToken);

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
    public async Task<EventDtoResponse> GetEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
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

        await _context.Events.AddAsync(eventEntity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return eventEntity.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<EventDtoResponse> UpdateEventAsync(Guid id, EventDto evtDto, CancellationToken cancellationToken = default)
    {
        // Дополнительная защита: на случай, если метод вызван без валидации модели
        ArgumentNullException.ThrowIfNull(evtDto, nameof(evtDto));

        var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Событие с ID {id} не найдено.");

        eventEntity.Update(evtDto.Title, evtDto.StartAt, evtDto.EndAt, evtDto.TotalSeats, evtDto.Description);
        await _context.SaveChangesAsync(cancellationToken);

        return eventEntity.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<bool> RemoveEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (eventEntity == null)
            throw new NotFoundException($"Событие с ID {id} не найдено.");
        _context.Events.Remove(eventEntity);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}