using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EventMgtApi.Presentation.Controllers;

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
    /// Возвращает информацию о брони по её идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор брони.</param>
    /// <returns>Текущее состояние брони.</returns>
    /// <response code="200">Бронь найдена и возвращена.</response>
    /// <response code="400">Некорректный формат идентификатора.</response>
    /// <response code="404">Бронь с указанным ID не найдена.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponseDto>> GetBookingById(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest("Идентификатор брони не может быть пустым.");

        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound();

        return Ok(booking);
    }
}