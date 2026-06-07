using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EventMgtApi.Presentation.Controllers;

/// <summary>
/// Контроллер для управления событиями через HTTP API.
/// Предоставляет операции: получение, добавление, обновление и удаление событий,
/// а также создание бронирований на события.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService; 

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EventsController"/>.
    /// </summary>
    /// <param name="eventService">Сервис для управления событиями. Не должен быть null.</param>
    /// <param name="bookingService">Сервис для управления бронированиями. Не должен быть null.</param>
    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
    }

    /// <summary>
    /// Возвращает список всех событий.
    /// </summary>
    /// <returns>
    /// HTTP 200 с коллекцией <see cref="EventDtoResponse"/>, 
    /// представляющей все события в системе.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<EventDtoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<EventDtoResponse>>> GetEvents(
        [FromQuery] string? title = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _eventService.GetEventsAsync(title, from, to, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Возвращает событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события. Должен быть в формате GUID.</param>
    /// <returns>
    /// Возвращает <see cref="EventDtoResponse"/> с данными события (HTTP 200), 
    /// Если событие не найдено - возвращает 404 (NotFound);
    /// При ошибке валидации входных данных — 400 (Bad Request).
    /// </returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDtoResponse>> GetEvent(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest("Идентификатор события не может быть пустым.");

        var evt = await _eventService.GetEventAsync(id);

        return Ok(evt);
    }

    /// <summary>
    /// Добавляет новое событие.
    /// </summary>
    /// <param name="evtDto">Модель события, переданная в теле запроса. Не должна быть null.</param>
    /// <returns>
    /// Возвращает <see cref="EventDtoResponse"/> с кодом 201 (Created) и URL нового ресурса в заголовке <c>Location</c>.
    /// При ошибке валидации входных данных — 400 (Bad Request).
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(EventDtoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDtoResponse>> AddEvent([FromBody] EventDto evtDto)
    {
        if (evtDto == null)
            return BadRequest("Тело запроса не может быть null.");

        var addedEvent = await _eventService.AddEventAsync(evtDto);

        return CreatedAtAction(
                nameof(GetEvent),
                new { id = addedEvent.Id },
                addedEvent);
    }

    /// <summary>
    /// Обновляет существующее событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо обновить.</param>
    /// <param name="evtDto">Новые данные события.</param>
    /// <returns>
    /// Возвращает <see cref="EventDtoResponse"/> с обновлёнными данными (HTTP 200), 
    /// если событие найдено и успешно изменено; 
    /// иначе — HTTP 404 (Not Found). 
    /// При ошибке валидации входных данных — 400 (Bad Request).
    /// </returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDtoResponse>> UpdateEvent(Guid id, [FromBody] EventDto evtDto)
    {
        if (id == Guid.Empty)
            return BadRequest("Идентификатор события не может быть пустым.");

        if (evtDto == null)
            return BadRequest("Тело запроса не может быть null.");

        var updatedEvent = await _eventService.UpdateEventAsync(id, evtDto);

        return Ok(updatedEvent);
    }

    /// <summary>
    /// Удаляет событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события для удаления.</param>
    /// <returns>
    /// Возвращает HTTP 204 (No Content), если событие успешно удалено; 
    /// HTTP 404 (Not Found), если событие с указанным ID не найдено; 
    /// При ошибке валидации входных данных — 400 (Bad Request).
    /// </returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveEvent(Guid id)
    {
        await _eventService.RemoveEventAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Создаёт новую бронь на указанное событие.
    /// </summary>
    /// <param name="id">Идентификатор события, на которое создаётся бронь.</param>
    /// <param name="request">DTO с данными для создания брони (заглушка).</param>
    /// <returns>Информация о созданной брони.</returns>
    /// <response code="202">Бронь успешно создана. Возвращён объект и заголовок Location.</response>
    /// <response code="400">Некорректный запрос.</response>
    /// <response code="404">Событие с указанным ID не найдено.</response>
    [HttpPost("{id:guid}/book")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponseDto>> CreateBookingForEvent
            (Guid id,
            [FromBody] CreateBookingRequestDto? request = null) // закладка на будущее
    {
        if (id == Guid.Empty)
            return BadRequest("Идентификатор события не может быть пустым.");

        var booking = await _bookingService.CreateBookingAsync(id);

        var locationUri = Url.Action("GetBookingById", "Bookings", new { id = booking.Id }, Request.Scheme);
        Response.Headers.Location = locationUri;

        return Accepted(booking);
    }
}