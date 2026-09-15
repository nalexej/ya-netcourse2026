using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Contracts.Events.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventMgtApi.EventsService.Web.Controllers;

/// <summary>
/// Контроллер для управления событиями через HTTP API.
/// Предоставляет операции: получение, добавление, обновление и удаление событий,
/// а также создание бронирований на события.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="EventsController"/>.
    /// </summary>
    /// <param name="eventService">Сервис для управления событиями. Не должен быть null.</param>
    public EventsController(IEventService eventService)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    /// <summary>
    /// Возвращает список всех событий.
    /// </summary>
    /// <returns>
    /// HTTP 200 с коллекцией <see cref="EventDtoResponse"/>, 
    /// представляющей все события в системе.
    /// </returns>
    [AllowAnonymous]
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
    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDtoResponse>> GetEvent(Guid id)
    {
        if (id == Guid.Empty)
            return BadRequest("Идентификатор события не может быть пустым.");

        var evt = await _eventService.GetEventAsync(id);

        return Ok(evt);
    }

    /// <summary>
    /// Возвращает топ-10 самых популярных событий по проценту продаж.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("top")]
    [ProducesResponseType(typeof(IEnumerable<TopEventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TopEventDto>>> GetTopEvents()
    {
        var topEvents = await _eventService.GetTopEventsAsync(10);
        return Ok(topEvents);
    }

    /// <summary>
    /// Добавляет новое событие.
    /// </summary>
    /// <param name="evtDto">Модель события, переданная в теле запроса. Не должна быть null.</param>
    /// <returns>
    /// Возвращает <see cref="EventDtoResponse"/> с кодом 201 (Created) и URL нового ресурса в заголовке <c>Location</c>.
    /// При ошибке валидации входных данных — 400 (Bad Request).
    /// </returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(EventDtoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] //требует аутентификации
    [ProducesResponseType(StatusCodes.Status403Forbidden)] //требует роли админа
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
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] //требует аутентификации
    [ProducesResponseType(StatusCodes.Status403Forbidden)] //требует роли админа
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] //требует аутентификации
    [ProducesResponseType(StatusCodes.Status403Forbidden)] //требует роли админа
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEvent(Guid id)
    {
        await _eventService.RemoveEventAsync(id);
        return NoContent();
    }
}