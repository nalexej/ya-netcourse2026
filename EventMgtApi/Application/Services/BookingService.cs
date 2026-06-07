using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Extensions;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace EventMgtApi.Application.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly object _bookingLock = new();

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingService"/>.
    /// </summary>
    /// <param name="bookingRepository">Репозиторий бронирований.</param>
    /// <param name="eventRepository">Репозиторий событий.</param>
    public BookingService(
        IBookingRepository bookingRepository,
        IEventRepository eventRepository)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> CreateBookingAsync(Guid eventId)
    {
        Event? @event;
        if (eventId == Guid.Empty)
            throw new ArgumentException("Идентификатор события не может быть пустым.", nameof(eventId));

        lock (_bookingLock)
        {
            // Получаем событие из репозитория
            @event =  _eventRepository.GetById(eventId);
            if (@event is null)
                throw new NotFoundException($"Событие с ID {eventId} не найдено.");

            // Пытаемся зарезервировать место
            if (!@event.TryReserveSeats())
                throw new NoAvailableSeatsException("Нет доступных мест для данного события.");

            // Обновляем событие в репозитории
            _eventRepository.Update(@event);

            // Создаём бронь
            var booking = new Booking(eventId);

            // Сохраняем бронь
            _bookingRepository.Add(booking); 

            return booking.ToDtoResponse();
        }
    }

    /// <inheritdoc />
    public async Task<BookingResponseDto> GetBookingByIdAsync(Guid bookingId)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Идентификатор брони не может быть пустым.", nameof(bookingId));
        
        var booking = await _bookingRepository.GetByIdAsync(bookingId);

        if (booking is null)
            throw new NotFoundException($"Бронь с ID {bookingId} не найдена.");

        return booking.ToDtoResponse();
    }
}
