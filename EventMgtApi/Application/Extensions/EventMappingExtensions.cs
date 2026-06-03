using EventMgtApi.Application.DTOs;
using EventMgtApi.Domain.Entities;

namespace EventMgtApi.Application.Extensions;

/// <summary>
/// Содержит методы расширения для преобразования моделей домена в DTO.
/// </summary>
public static class EventMappingExtensions
{
    /// <summary>
    /// Преобразует объект <see cref="Event"/> в <see cref="EventDtoResponse"/>.
    /// </summary>
    /// <param name="eventEntity">Исходный объект события. Не должен быть <see langword="null"/>.</param>
    /// <returns>Новый экземпляр <see cref="EventDtoResponse"/> с данными из <paramref name="eventEntity"/>.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="eventEntity"/> равен <see langword="null"/>.</exception>
    public static EventDtoResponse ToDtoResponse(this Event eventEntity)
    {
        ArgumentNullException.ThrowIfNull(eventEntity, nameof(eventEntity));
        return new EventDtoResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            StartAt = eventEntity.StartAt,
            EndAt = eventEntity.EndAt,
            TotalSeats = eventEntity.TotalSeats,
            AvailableSeats = eventEntity.AvailableSeats
        };
    }

    /// <summary>
    /// Преобразует коллекцию событий в список DTO.
    /// </summary>
    /// <param name="events">Коллекция событий для преобразования. Не должна быть <see langword="null"/>.</param>
    /// <returns>
    /// Новый экземпляр <see cref="List{T}"/>, содержащий объекты <see cref="EventDtoResponse"/>.
    /// Изменения в возвращаемом списке не влияют на исходную коллекцию.
    /// </returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="events"/> — <see langword="null"/>.</exception>
    public static List<EventDtoResponse> ToDtoList(this IEnumerable<Event> events)
    {
        ArgumentNullException.ThrowIfNull(events, nameof(events));
        return events.Select(e => e.ToDtoResponse()).ToList();
    }
}