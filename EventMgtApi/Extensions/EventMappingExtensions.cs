using EventMgtApi.Models;
using EventMgtApi.Models.Dto;

namespace EventMgtApi.Extensions;

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
            EndAt = eventEntity.EndAt
        };
    }
}