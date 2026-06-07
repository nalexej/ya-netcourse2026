
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Interfaces;

namespace EventMgtApi.Infrastructure.Repositories;

/// <summary>
/// Потокобезопасная реализация репозитория бронирований в памяти.
/// </summary>
public class InMemoryBookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

    /// <inheritdoc />
    public Task<IEnumerable<Booking>> GetAllAsync()
    {
        var bookings = _bookings.Values.ToList();
        return Task.FromResult<IEnumerable<Booking>>(bookings);
    }

    /// <inheritdoc />
    public Task<Booking?> GetByIdAsync(Guid id)
    {
        _bookings.TryGetValue(id, out var booking);
        return Task.FromResult<Booking?>(booking);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Booking>> GetByEventIdAsync(Guid eventId)
    {
        if (eventId == Guid.Empty)
            return Task.FromResult<IEnumerable<Booking>>(Enumerable.Empty<Booking>());

        var bookings = _bookings.Values.Where(b => b.EventId == eventId).ToList();
        return Task.FromResult<IEnumerable<Booking>>(bookings);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status)
    {
        var bookings = _bookings.Values.Where(b => b.Status == status).ToList();
        return Task.FromResult<IEnumerable<Booking>>(bookings);
    }

    /// <inheritdoc />
    public Booking Add(Booking booking)
    {
        if (booking == null)
            throw new ArgumentNullException(nameof(booking));

        if (booking.Id == Guid.Empty)
            throw new ArgumentException("Бронь должна иметь валидный Id.", nameof(booking));

        _bookings[booking.Id] = booking;
        return booking;
    }

    /// <inheritdoc />
    public Task<Booking> AddAsync(Booking booking)
    {
        if (booking == null)
            throw new ArgumentNullException(nameof(booking));

        if (booking.Id == Guid.Empty)
            throw new ArgumentException("Бронь должна иметь валидный Id.", nameof(booking));

        _bookings[booking.Id] = booking;
        return Task.FromResult(booking);
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(Booking booking)
    {
        if (booking == null)
            throw new ArgumentNullException(nameof(booking));

        if (booking.Id == Guid.Empty)
            throw new ArgumentException("Бронь должна иметь валидный Id.", nameof(booking));

        // Проверяем существование
        if (!_bookings.ContainsKey(booking.Id))
            return Task.FromResult(false);

        // Полное замещение
        _bookings[booking.Id] = booking;
        return Task.FromResult(true);
    }
}