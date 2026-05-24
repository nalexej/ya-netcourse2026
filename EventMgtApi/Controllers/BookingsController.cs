using EventMgtApi.Exceptions;
using EventMgtApi.Models;
using EventMgtApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EventMgtApi.Controllers;

/// <summary>
/// Контроллер для управления бронированиями.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingsController"/>.
    /// </summary>
    /// <param name="bookingService">Сервис бронирований.</param>
    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Создаёт новую бронь для указанного события.
    /// </summary>
    /// <param name="eventId">Идентификатор события из URL.</param>
    /// <param name="request">DTO с данными для создания брони.</param>
    /// <returns>Информация о созданной брони.</returns>
    /// <response code="202">Бронь успешно создана. Возвращён объект и заголовок Location.</response>
    /// <response code="400">Некорректный запрос (например, пустой eventId).</response>
    /// <response code="404">Событие с указанным ID не найдено.</response>
    [HttpPost("events/{eventId:guid}/book")]
    [ProducesResponseType(typeof(BookingResponseDto), 202)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(
    [FromRoute] Guid eventId,
    [FromBody] CreateBookingRequestDto request) // закладка на будущее
    {
        if (eventId == Guid.Empty)
            return BadRequest("Идентификатор события не может быть пустым.");

        try
        {
            var booking = await _bookingService.CreateBookingAsync(eventId);

            var response = new BookingResponseDto
            {
                Id = booking.Id,
                EventId = booking.EventId,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };

            var locationUri = Url.Action(nameof(GetBookingById), "Bookings", new { id = booking.Id }, Request.Scheme);
            Response.Headers.Location = locationUri;

            return Accepted(response);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Возвращает информацию о брони по её идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор брони.</param>
    /// <returns>Текущее состояние брони.</returns>
    /// <response code="200">Бронь найдена и возвращена.</response>
    /// <response code="400">Некорректный формат идентификатора.</response>
    /// <response code="404">Бронь с указанным ID не найдена.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<BookingResponseDto>> GetBookingById(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest("Идентификатор брони не может быть пустым.");

        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound();

        var response = new BookingResponseDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        return Ok(response);
    }
}