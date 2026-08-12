using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Application.Interfaces;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly BookingOptions _bookingOptions;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingService"/>.
    /// </summary>
    /// <param name="eventRepository">Репозиторий для доступа к данным событий. Не должен быть <see langword="null"/>.</param>
    /// <param name="bookingRepository">Репозиторий для доступа к данным бронирований. Не должен быть <see langword="null"/>.</param>
    /// <param name="bookingOptionsAccessor">Источник доступа к конфигурационным параметтам бронирований. Не должен быть <see langword="null"/>.</param>
    public BookingService(IEventRepository eventRepository, IBookingRepository bookingRepository, IOptions<BookingOptions> bookingOptionsAccessor)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
        _bookingOptions = bookingOptionsAccessor.Value;
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

            // 1. Запрет на бронирование события, которое уже началось или завершилось (400)
            if (@event.StartAt <= DateTime.UtcNow && @event.EndAt >= DateTime.UtcNow)
            {
                throw new BookingPastEventException("Нельзя забронировать событие, которое уже началось.");
            }

            if (@event.EndAt < DateTime.UtcNow)
            {
                throw new BookingPastEventException("Нельзя забронировать событие, которое уже завершилось.");
            }

            // 2. Проверка лимита активных броней (максимум 10) (409)
            int activeBookingsCount = await _bookingRepository.GetActiveBookingsCountAsync(userId, cancellationToken);

            var MaxActiveBookings = _bookingOptions.MaxActiveBookings;
            if (activeBookingsCount >= MaxActiveBookings)
            {
                throw new TooManyActiveBookingsException($"У пользователя уже есть {activeBookingsCount} активных броней. Предел: {MaxActiveBookings}.");
            }

            // 3. Проверка наличия мест
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

        // Проверка прав доступа
        // Пользователь может отменить только свою бронь, если он не админ
        if (booking.UserId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("Недостаточно прав для отмены этой брони. Вы можете отменить только свою бронь.");
        }

        // Проверяем, что событие не прошло
        if (booking.Event != null && booking.Event.StartAt <= DateTime.UtcNow)
            throw new BookingPastEventException("Нельзя отменить бронь на прошедшее событие.");

        // Вызываем метод отмены на сущности (внутри валидация статуса)
        booking.Cancel();
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        // Освобождаем место
        var eventId = booking.EventId;
        var evt = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Событие с ID {eventId} не найдено.");
        evt.ReleaseSeats();
        await _eventRepository.SaveChangesAsync(cancellationToken);

        return booking.ToDtoResponse();
    }
}
