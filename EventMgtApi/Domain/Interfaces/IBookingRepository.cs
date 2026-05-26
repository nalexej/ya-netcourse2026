using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;

namespace EventMgtApi.Domain.Interfaces;

/// <summary>
/// Интерфейс репозитория для управления бронированиями.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Возвращает все брони (без фильтрации).
    /// </summary>
    /// <returns>Полный список всех броней в системе.</returns>
    Task<IEnumerable<Booking>> GetAllAsync();

    /// <summary>
    /// Асинхронно получает бронь по идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор брони.</param>
    /// <returns>Найденная бронь; иначе <see langword="null"/>.</returns>
    Task<Booking?> GetByIdAsync(Guid id);

    /// <summary>
    /// Асинхронно получает все брони для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события.</param>
    /// <returns>Список броней, связанных с событием.</returns>
    Task<IEnumerable<Booking>> GetByEventIdAsync(Guid eventId);

    /// <summary>
    /// Асинхронно получает все брони определённого статуса.
    /// </summary>
    /// <param name="status">Статус брони (например, <see cref="BookingStatus.Pending"/>).</param>
    /// <returns>Список броней с указанным статусом.</returns>
    Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status);

    /// <summary>
    /// Асинхронно добавляет новую бронь.
    /// </summary>
    /// <param name="booking">Бронь для добавления. Должна иметь валидный <see cref="Booking.Id"/>.</param>
    /// <returns>Добавленная бронь.</returns>
    Task<Booking> AddAsync(Booking booking);

    /// <summary>
    /// Асинхронно обновляет существующую бронь.
    /// </summary>
    /// <param name="booking">Объект брони с обновлёнными данными. Должен существовать в хранилище.</param>
    /// <returns><c>true</c>, если обновление прошло успешно; иначе <c>false</c>.</returns>
    Task<bool> UpdateAsync(Booking booking);
}