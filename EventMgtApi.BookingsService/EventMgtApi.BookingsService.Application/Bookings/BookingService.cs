using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.BookingsService.Application.Extensions;
using EventMgtApi.BookingsService.Application.Persistence;
using EventMgtApi.BookingsService.Domain.Entities;
using EventMgtApi.BookingsService.Domain.Exceptions;
using EventMgtApi.BookingsService.Domain.Options;
using EventMgtApi.Contracts.Bookings.DTOs;
using EventMgtApi.Contracts.ServiceInteraction;
using EventMgtApi.Contracts.ServiceInteraction.ServiceEvents;
using Microsoft.Extensions.Options;

namespace EventMgtApi.BookingsService.Application.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly BookingOptions _bookingOptions;
    private readonly IEventPublisher _eventPublisher;

    public BookingService(
        IBookingRepository bookingRepository,
        IOptions<BookingOptions> bookingOptionsAccessor,
        IEventPublisher eventPublisher)
    {
        _bookingRepository = bookingRepository;
        _bookingOptions = bookingOptionsAccessor.Value;
        _eventPublisher = eventPublisher;
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

        int activeCount = await _bookingRepository.GetActiveBookingsCountAsync(userId, cancellationToken);
        if (activeCount >= _bookingOptions.MaxActiveBookings)
            throw new TooManyActiveBookingsException(
                $"У пользователя уже есть {activeCount} активных броней. Предел: {_bookingOptions.MaxActiveBookings}.");

        // Создаём бронь
        var booking = new Booking(eventId, userId);
        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        return booking.ToDtoResponse();
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

        booking.Cancel();
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        var cancelledEvent = new BookingCancelled(
            bookingId: booking.Id,
            eventId: booking.EventId,
            userId: booking.UserId,
            seatsCount: 1,
            cancelledAt: booking.ProcessedAt!.Value
        );

        await _eventPublisher.PublishAsync(
            cancelledEvent,
            key: booking.EventId.ToString(),
            ct: cancellationToken
        );

        return booking.ToDtoResponse();
    }
}
