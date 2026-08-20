
using EventMgtApi.BookingsService.Domain.Entities;
using EventMgtApi.Contracts.Enums;

namespace EventMgtApi.BookingsService.Application.Persistence;

/// <summary>
/// Интерфейс репозитория для управления бронированиями.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Возвращает все брони (без фильтрации).
    /// </summary>
    /// <returns>Полный список всех броней в системе.</returns>
    /// <param name="ct">Токен отмены.</param>
    Task<IEnumerable<Booking>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Асинхронно получает бронь по идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор брони.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Найденная бронь; иначе <see langword="null"/>.</returns>
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно получает все брони для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Список броней, связанных с событием.</returns>
    Task<IEnumerable<Booking>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно получает все брони определённого статуса.
    /// </summary>
    /// <param name="status">Статус брони (например, <see cref="BookingStatus.Pending"/>).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Список броней с указанным статусом.</returns>
    Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно получает идентификаторы броней, ожидающих обработки.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Список индентификаторов броней, ожидающих обработки.</returns>
    Task<IEnumerable<Guid>> GetPendingBookingsIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Асинхронно получает количество активных бронирований для указанного пользователя.
    /// </summary>
    Task<int> GetActiveBookingsCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно добавляет новую бронь.
    /// </summary>
    /// <param name="booking">Бронь для добавления. Должна иметь валидный <see cref="Booking.Id"/>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    Task AddAsync(Booking booking, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно сохраняет изменения в базу данных.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Задача, представляющая операцию сохранения.</returns>
    Task SaveChangesAsync(CancellationToken ct = default);
}