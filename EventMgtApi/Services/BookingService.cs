using EventMgtApi.Exceptions;
using EventMgtApi.Models;
using EventMgtApi.Repositories;
using EventMgtApi.Services;
using System;
using System.Threading.Tasks;

namespace EventMgtApi.Services;

/// <summary>
/// Сервис для управления бронированиями.
/// </summary>
public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;

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
    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Идентификатор события не может быть пустым.", nameof(eventId));

        // Проверяем, существует ли событие
        //var @event = await _eventRepository.GetByIdAsync(eventId);
        var @event = _eventRepository.GetById(eventId);
        if (@event == null)
            throw new NotFoundException($"Событие с ID {eventId} не найдено.");

        // Создаём бронь
        var booking = new Booking(eventId);

        // Сохраняем
        await _bookingRepository.AddAsync(booking);

        return booking;
    }

    /// <inheritdoc />
    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Идентификатор брони не может быть пустым.", nameof(bookingId));

        return await _bookingRepository.GetByIdAsync(bookingId);
    }
}