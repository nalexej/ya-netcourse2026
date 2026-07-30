using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Application.Interfaces;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IEventRepository _eventrepository;
    private readonly IBookingRepository _bookingrepository;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingService"/>.
    /// </summary>
    /// <param name="eventRepository">Репозиторий для доступа к данным событий. Не должен быть <see langword="null"/>.</param>
    /// <param name="bookingRepository">Репозиторий для доступа к данным бронирований. Не должен быть <see langword="null"/>.</param>
    public BookingService(IEventRepository eventRepository, IBookingRepository bookingRepository)
    {
        _eventrepository = eventRepository;
        _bookingrepository = bookingRepository;
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
            @event = await _eventrepository.GetByIdAsync(eventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с ID {eventId} не найдено.");

            // Пытаемся зарезервировать место
            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("Нет доступных мест для данного события.");

            // Создаём бронь
            var booking = new Booking(eventId);

            await _bookingrepository.AddAsync(booking, cancellationToken);
            await _bookingrepository.SaveChangesAsync(cancellationToken);

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

        var booking = await _bookingrepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронь с ID {bookingId} не найдена.");

        return booking.ToDtoResponse();
    }
}
