using EventMgtApi.Models;
using EventMgtApi.Models.Dto;

namespace EventMgtApi.Services;

/// <summary>
/// Реализация сервиса управления событиями.
/// </summary>
public class EventService : IEventService
{
    /// <summary>
    /// Коллекция событий. Предназначена только для внутреннего использования.
    /// </summary>
    /// <remarks>
    /// Доступ к коллекции предоставляется через публичные методы (например, <see cref="GetEvents"/>).
    /// Прямая модификация может нарушить инварианты, если не сопровождается дополнительной логикой.
    /// </remarks>
    private readonly List<Event> _events = [];
    
    /// <summary>
    /// Объект блокировки.
    /// </summary>
    private readonly Lock _lock = new();

    /// <summary>
    /// Возвращает копию списка всех событий.
    /// </summary>
    /// <returns>
    /// Новый список, содержащий все текущие события. 
    /// Изменения в возвращаемом списке не влияют на внутреннее состояние.
    /// </returns>
    public List<Event> GetEvents()
    {
        lock (_lock)
            return [.. _events];
    }

    /// <summary>
    /// Возвращает событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события для поиска.</param>
    /// <returns>
    /// Найденное событие, если оно существует; иначе — <see langword="null"/>.
    /// </returns>
    public Event? GetEvent(Guid id)
    {
        lock (_lock)
        {
            var original = _events.FirstOrDefault(e => e.Id == id);
            return original is null ? null : new Event
            {
                Id = original.Id,
                Title = original.Title,
                Description = original.Description,
                StartAt = original.StartAt,
                EndAt = original.EndAt
            };
        }
    }

    /// <summary>
    /// Добавляет новое событие.
    /// </summary>
    /// <param name="evtDto">Данные события, которое необходимо добавить. Не должно быть null.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если параметр <paramref name="evtDto"/> равен null.</exception>
    public Event AddEvent(EventDto evtDto)
    {
        ArgumentNullException.ThrowIfNull(evtDto, nameof(evtDto));

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = evtDto.Title,
            Description = evtDto.Description ?? string.Empty,
            StartAt = evtDto.StartAt,
            EndAt = evtDto.EndAt
        };

        lock (_lock)
            _events.Add(eventEntity);

        return eventEntity;
    }

    /// <summary>
    /// Обновляет существующее событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо обновить.</param>
    /// <param name="evtDto">Новые данные события. Не должно быть null.</param>
    /// <returns>
    /// Возвращает обновлённое событие, если оно найдено и успешно обновлено; иначе — null.
    /// </returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если параметр <paramref name="evtDto"/> равен null.</exception>
    public Event? UpdateEvent(Guid id, EventDto evtDto)
    {
        ArgumentNullException.ThrowIfNull(evtDto, nameof(evtDto));

        Event? existingEvent;
        lock (_lock)
        {
            existingEvent = _events.FirstOrDefault(evt => evt.Id == id);
            if (existingEvent != null)
            {
                existingEvent.Title = evtDto.Title;
                existingEvent.Description = evtDto.Description ?? string.Empty;
                existingEvent.StartAt = evtDto.StartAt;
                existingEvent.EndAt = evtDto.EndAt;
            }
        }

        return existingEvent;
    }

    /// <summary>
    /// Удаляет событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо удалить.</param>
    /// <returns>
    /// <see langword="true"/>, если событие было найдено и удалено; 
    /// иначе — <see langword="false"/>.
    /// </returns>
    public bool RemoveEvent(Guid id)
    {
        lock (_lock)
            return _events.RemoveAll(e => e.Id == id) > 0;
    }
}