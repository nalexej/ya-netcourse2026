using EventMgtApi.Models;
using EventMgtApi.Models.Dto;
using EventMgtApi.Exceptions;
using EventMgtApi.Extensions;
using EventMgtApi.Repositories;

namespace EventMgtApi.Services;

/// <summary>
/// Реализация сервиса управления событиями.
/// Предоставляет бизнес-логику для операций: получение, добавление, обновление, удаление событий.
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

    /// <summary>
    /// Возвращает копию списка всех событий.
    /// </summary>
    /// <param name="title">Поиск по названию (частичное совпадение, регистронезависимо)</param>
    /// <param name="from">Фильтр: события, которые начинаются не раньше указанной даты</param>
    /// <param name="to">Фильтр: события, которые заканчиваются не позже указанной даты</param>
    /// <param name="page">Номер страницы (начинается с 1). Значение по умолчанию — 1. Если передано значение меньше 1, будет использовано 1.</param>
    /// <param name="pageSize">Количество элементов на странице. Значение по умолчанию — 10, максимальное значение — 100.</param>
    /// <returns>
    /// Экземпляр <see cref="PaginatedResult{T}"/>, содержащий отфильтрованные и разбитые на страницы события.
    /// </returns>
    public PaginatedResult<EventDtoResponse> GetEvents(
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
        var allEvents = _repository.GetAll();

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

    /// <summary>
    /// Возвращает событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события для поиска.</param>
    /// <returns>
    /// Копия найденного события в виде <see cref="EventDtoResponse"/>.</returns>
    /// <exception cref="NotFoundException">
    /// Исключение выбрасывается, если событие с указанным <paramref name="id"/> не найдено.
    /// </exception>   
    public EventDtoResponse GetEvent(Guid id)
    {
        var eventEntity = _repository.GetById(id);

        if (eventEntity is null)
            throw new NotFoundException($"Событие с ID {id} не найдено.");

        return eventEntity.ToDtoResponse();
    }

    /// <summary>
    /// Добавляет новое событие.
    /// </summary>
    /// <param name="evtDto">Данные события, которое необходимо добавить. Не должно быть null.</param>
    /// <returns>Возвращает копию добавленного события в виде <see cref="EventDtoResponse"/>.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если параметр <paramref name="evtDto"/> равен null.</exception>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если:
    /// <list type="bullet">
    ///   <item><description>Заголовок пуст или состоит только из пробелов.</description></item>
    ///   <item><description>Дата начала не меньше даты окончания.</description></item>
    /// </list>
    /// </exception>
    public EventDtoResponse AddEvent(EventDto evtDto)
    {
        // Дополнительная защита: на случай, если метод вызван без валидации модели
        ArgumentNullException.ThrowIfNull(evtDto, nameof(evtDto));

        if (string.IsNullOrWhiteSpace(evtDto.Title))
            throw new ValidationException("Заголовок обязателен.");

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = evtDto.Title,
            Description = evtDto.Description ?? string.Empty,
            StartAt = evtDto.StartAt ?? throw new ValidationException("Дата начала обязательна."), // Доп. защита
            EndAt = evtDto.EndAt ?? throw new ValidationException("Дата окончания обязательна.") // Доп. защита
        };

        if (eventEntity.StartAt >= eventEntity.EndAt)
            throw new ValidationException("Дата начала должна быть раньше даты окончания."); // Доп. защита

        _repository.Add(eventEntity);

        return eventEntity.ToDtoResponse();
    }

    /// <summary>
    /// Обновляет существующее событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор события для обновления.</param>
    /// <param name="evtDto">Новые данные события. Не должен быть <see langword="null"/>.</param>
    /// <returns>Возвращает обновлённую копию события в виде <see cref="EventDtoResponse"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если параметр <paramref name="evtDto"/> равен <see langword="null"/>.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если:
    /// <list type="bullet">
    ///   <item><description>Заголовок пуст или состоит только из пробелов.</description></item>
    ///   <item><description>Дата начала не меньше даты окончания.</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="NotFoundException">
    /// Выбрасывается, если событие с указанным <paramref name="id"/> не найдено.
    /// </exception>
    public EventDtoResponse UpdateEvent(Guid id, EventDto evtDto)
    {
        // Дополнительная защита: на случай, если метод вызван без валидации модели
        ArgumentNullException.ThrowIfNull(evtDto, nameof(evtDto));

        if (string.IsNullOrWhiteSpace(evtDto.Title))
            throw new ValidationException("Заголовок обязателен.");

        var updatedEvent = new Event
        {
            Id = id,
            Title = evtDto.Title,
            Description = evtDto.Description ?? string.Empty,
            StartAt = evtDto.StartAt ?? throw new ValidationException("Дата начала обязательна."), // Доп. защита
            EndAt = evtDto.EndAt ?? throw new ValidationException("Дата окончания обязательна.") // Доп. защита
        };

        if (updatedEvent.StartAt >= updatedEvent.EndAt)
            throw new ValidationException("Дата начала должна быть раньше даты окончания."); // Доп. защита

        var success = _repository.Update(updatedEvent);
        if (!success)
            throw new NotFoundException($"Событие с ID {id} не найдено.");

        return updatedEvent.ToDtoResponse();
    }

    /// <summary>
    /// Удаляет событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо удалить.</param>
    /// <exception cref="NotFoundException">
    /// Выбрасывается, если событие с указанным <paramref name="id"/> не найдено.
    /// </exception>
    public void RemoveEvent(Guid id)
    {
        var success = _repository.Delete(id);
        if (!success)
            throw new NotFoundException($"Событие с ID {id} не найдено.");
    }
}