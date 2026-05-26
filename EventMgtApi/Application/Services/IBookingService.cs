using System;
using System.Threading.Tasks;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Интерфейс сервиса для управления бронированиями.
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Асинхронно создаёт новую бронь для указанного события.
    /// Статус брони устанавливается в <see cref="BookingStatus.Pending"/>.
    /// </summary>
    /// <param name="eventId">Идентификатор события, для которого создаётся бронь.</param>
    /// <returns>Созданная бронь.</returns>
    Task<Booking> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// Асинхронно получает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Уникальный идентификатор брони.</param>
    /// <returns>Найденная бронь или <c>null</c>, если не найдена.</returns>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);
}