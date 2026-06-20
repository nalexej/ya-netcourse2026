using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingService"/>.
    /// </summary>
    /// <param name="context">Контекст базы данных</param>
    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        Event? @event;
        if (eventId == Guid.Empty)
            throw new ArgumentException("Идентификатор события не может быть пустым.", nameof(eventId));

        await BookingLock.WaitAsync(cancellationToken);
        try
        {
            @event = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с ID {eventId} не найдено.");

            // Пытаемся зарезервировать место
            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("Нет доступных мест для данного события.");

            // Создаём бронь
            var booking = new Booking(eventId);
            await _context.Bookings.AddAsync(booking, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return booking.ToDtoResponse();
        }
        finally
        {
            BookingLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Идентификатор брони не может быть пустым.", nameof(bookingId));
        
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронь с ID {bookingId} не найдена.");

        return booking.ToDtoResponse();
    }
}
