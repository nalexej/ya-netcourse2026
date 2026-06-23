using EventMgtApi.Application.DTOs;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Enums;
using System;
using System.Threading.Tasks;

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
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO созданной брони.</returns>
    Task<BookingResponseDto> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно получает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Уникальный идентификатор брони.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO найденной брони.</returns>
    /// <exception cref="NotFoundException">Выбрасывается, если бронь с указанным ID не найдена.</exception>
    Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}