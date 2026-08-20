using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Contracts.Bookings.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventMgtApi.BookingsService.Web.Controllers;

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
    /// <response code="401">Требуется аутентификация.</response>
    /// <response code="404">Бронь с указанным ID не найдена.</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponseDto>> GetBookingById(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest("Идентификатор брони не может быть пустым.");

        // Извлекаем ID пользователя из claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return BadRequest();
        }

        // Проверяем роль администратора
        var isAdmin = User.IsInRole("Admin");

        var booking = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin);

        if (booking == null)
            return NotFound();

        return Ok(booking);
    }

    /// <summary>
    /// Отменяет бронь по идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор брони.</param>
    /// <returns>DTO отменённой брони.</returns>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelBooking(Guid id)
    {
        // Извлекаем ID пользователя из claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return BadRequest();
        }

        // Проверяем роль администратора
        var isAdmin = User.IsInRole("Admin");

        var booking = await _bookingService.CancelBookingAsync(id, userId, isAdmin);
        return NoContent();
    }

    /// <summary>
    /// Создаёт новую бронь на указанное событие.
    /// </summary>
    /// <param name="eventId">Идентификатор события, на которое создаётся бронь.</param>
    /// <param name="request">DTO с данными для создания брони (заглушка).</param>
    /// <returns>Информация о созданной брони.</returns>
    /// <response code="201">Бронь успешно создана. Возвращён объект и заголовок Location.</response>
    /// <response code="400">Событие уже началось/ некорректный запрос.</response>
    /// <response code="401">Требуется аутентификация.</response>
    /// <response code="404">Событие с указанным ID не найдено.</response>
    /// <response code="409">Нет доступных мест или превышен лимит броней.</response>
    [Authorize]
    [HttpPost("/api/Events/{eventId:guid}/book")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] //событие уже началось
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] //требует аутентификации
    [ProducesResponseType(StatusCodes.Status404NotFound)] // событие не найдено
    [ProducesResponseType(StatusCodes.Status409Conflict)] //нет доступных мест или превышен лимит броней
    public async Task<ActionResult<BookingResponseDto>> CreateBookingForEvent
            (Guid eventId,
            [FromBody] CreateBookingRequestDto request)
    {
        if (eventId == Guid.Empty)
            return BadRequest("Идентификатор события не может быть пустым.");

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());
        var isAdmin = User.IsInRole("Admin");

        var booking = await _bookingService.CreateBookingAsync(eventId, userId);

        return Created(
            Url.Action("GetBookingById", "Bookings", new { id = booking.Id }, Request.Scheme)!,
            booking);
    }
}
