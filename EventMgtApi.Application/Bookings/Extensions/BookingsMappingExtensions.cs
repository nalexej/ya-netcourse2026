using EventMgtApi.Application.DTOs;
using EventMgtApi.Domain.Entities;

namespace EventMgtApi.Application.Extensions;

/// <summary>
/// Содержит методы расширения для преобразования моделей домена в DTO.
/// </summary>
public static class BookingsMappingExtensions
{
    /// <summary>
    /// Преобразует объект <see cref="Booking"/> в <see cref="BookingResponseDto"/>.
    /// </summary>
    /// <param name="bookingEntity">Исходный объект брони. Не должен быть <see langword="null"/>.</param>
    /// <returns>Новый экземпляр <see cref="BookingResponseDto"/> с данными из <paramref name="bookingEntity"/>.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="bookingEntity"/> равен <see langword="null"/>.</exception>
    public static BookingResponseDto ToDtoResponse(this Booking bookingEntity)
    {
        ArgumentNullException.ThrowIfNull(bookingEntity, nameof(bookingEntity));
        return new BookingResponseDto
        {
            Id = bookingEntity.Id,
            EventId = bookingEntity.EventId,
            UserId = bookingEntity.UserId,
            Status = bookingEntity.Status,
            CreatedAt = bookingEntity.CreatedAt,
            ProcessedAt = bookingEntity.ProcessedAt
        };
    }
}
