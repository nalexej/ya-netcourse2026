using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Application.Abstractions.Persistence.Repositories;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Options;
using Microsoft.Extensions.Options;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly BookingOptions _bookingOptions;

    public BookingService(
        IEventRepository eventRepository,
        IBookingRepository bookingRepository,
        IOptions<BookingOptions> bookingOptionsAccessor)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
        _bookingOptions = bookingOptionsAccessor.Value;
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> CreateBookingAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Идентификатор события не может быть пустым.", nameof(eventId));

        if (userId == Guid.Empty)
            throw new ArgumentException("Идентификатор пользователя не может быть пустым.", nameof(userId));

        return await _eventRepository.ExecuteWithConcurrencyRetryAsync(
            async () =>
            {
                // БЛОКИРУЕМ строку события — SELECT ... FOR UPDATE
                var @event = await _eventRepository.GetWithLockAsync(eventId, cancellationToken)
                    ?? throw new NotFoundException($"Событие с ID {eventId} не найдено.");

                // Проверка: событие не началось
                if (@event.StartAt <= DateTime.UtcNow)
                    throw new BookingPastEventException("Нельзя забронировать событие, которое уже началось.");

                if (@event.EndAt < DateTime.UtcNow)
                    throw new BookingPastEventException("Нельзя забронировать событие, которое уже завершилось.");

                // Проверка лимита активных броней
                int activeCount = await _bookingRepository.GetActiveBookingsCountAsync(userId, cancellationToken);
                if (activeCount >= _bookingOptions.MaxActiveBookings)
                    throw new TooManyActiveBookingsException(
                        $"У пользователя уже есть {activeCount} активных броней. Предел: {_bookingOptions.MaxActiveBookings}.");

                // Резервируем место (теперь безопасно — строка заблокирована)
                if (!@event.TryReserveSeats())
                    throw new NoAvailableSeatsException("Нет доступных мест для данного события.");

                // Создаём бронь
                var booking = new Booking(eventId, userId);
                await _bookingRepository.AddAsync(booking, cancellationToken);
                await _bookingRepository.SaveChangesAsync(cancellationToken);

                return booking.ToDtoResponse();
            },
            maxRetries: 3,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> GetBookingByIdAsync(
        Guid bookingId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Идентификатор брони не может быть пустым.", nameof(bookingId));

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронь с ID {bookingId} не найдена.");

        if (booking.UserId != currentUserId && !isAdmin)
            throw new ForbiddenException(
                "Недостаточно прав для запроса данных данной брони: Вы можете запросить только свою бронь.");

        return booking.ToDtoResponse();
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> CancelBookingAsync(
        Guid bookingId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Идентификатор брони не может быть пустым.", nameof(bookingId));

        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронь с ID {bookingId} не найдена.");

        if (booking.UserId != currentUserId && !isAdmin)
            throw new ForbiddenException(
                "Недостаточно прав для отмены этой брони. Вы можете отменить только свою бронь.");

        if (booking.Event != null && booking.Event.StartAt <= DateTime.UtcNow)
            throw new BookingPastEventException("Нельзя отменить бронь на прошедшее событие.");

        booking.Cancel();
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        var eventId = booking.EventId;
        var evt = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Событие с ID {eventId} не найдено.");
        evt.ReleaseSeats();
        await _eventRepository.SaveChangesAsync(cancellationToken);

        return booking.ToDtoResponse();
    }
}