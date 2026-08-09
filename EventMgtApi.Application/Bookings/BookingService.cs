using EventMgtApi.Application.Abstractions.Services;
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

    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingService"/>.
    /// </summary>
    /// <param name="eventRepository">Репозиторий для доступа к данным событий. Не должен быть <see langword="null"/>.</param>
    /// <param name="bookingRepository">Репозиторий для доступа к данным бронирований. Не должен быть <see langword="null"/>.</param>
    public BookingService(IEventRepository eventRepository, IBookingRepository bookingRepository)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        Event? @event;
        if (eventId == Guid.Empty)
            throw new ArgumentException("Идентификатор события не может быть пустым.", nameof(eventId));

        if (userId == Guid.Empty)
            throw new ArgumentException("Идентификатор пользователя не может быть пустым.", nameof(userId));

        await BookingLock.WaitAsync(cancellationToken);
        try
        {
            @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с ID {eventId} не найдено.");

            // Проверка наличия мест
            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("Нет доступных мест для данного события.");

            // Создаём бронь с привязкой к пользователю
            var booking = new Booking(eventId, userId);

            await _bookingRepository.AddAsync(booking, cancellationToken);
            await _bookingRepository.SaveChangesAsync(cancellationToken);

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

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронь с ID {bookingId} не найдена.");

        return booking.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> CancelBookingAsync(Guid bookingId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Идентификатор брони не может быть пустым.", nameof(bookingId));

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронь с ID {bookingId} не найдена.");

        // Вызываем метод отмены на сущности (внутри валидация статуса)
        booking.Cancel();

        await _bookingRepository.SaveChangesAsync(cancellationToken);

        return booking.ToDtoResponse();
    }
}
