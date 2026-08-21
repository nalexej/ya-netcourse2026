using EventMgtApi.Contracts.Bookings.DTOs;

namespace EventMgtApi.Application.Abstractions.Services;

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
    /// <param name="userId">Идентификатор пользователя, создающего бронь.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO созданной брони.</returns>
    Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно получает бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Уникальный идентификатор брони.</param>
    /// <param name="userId">Идентификатор текущего пользователя.</param>
    /// <param name="isAdmin">Флаг, является ли пользователь администратором.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO найденной брони.</returns>
    /// <exception cref="Domain.Exceptions.NotFoundException">Выбрасывается, если бронь с указанным ID не найдена.</exception>
    Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отменяет бронь по идентификатору.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони.</param>
    /// <param name="userId">Идентификатор текущего пользователя.</param>
    /// <param name="isAdmin">Флаг, является ли пользователь администратором.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO отменённой брони.</returns>
    Task<BookingResponseDto> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);

}
