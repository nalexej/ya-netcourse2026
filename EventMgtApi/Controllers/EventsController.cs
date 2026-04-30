using EventMgtApi.Models;
using EventMgtApi.Models.Dto;
using EventMgtApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventMgtApi.Controllers;

/// <summary>
/// Контроллер для управления событиями через HTTP API.
/// Предоставляет операции: получение, добавление, обновление и удаление событий.
/// </summary>
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
    /// HTTP 200 (OK) с коллекцией событий.
    /// </returns>
    [HttpGet]
    public IActionResult GetEvents()
    {
        var events = _eventService.GetEvents();
        return Ok(events);
    }

    /// <summary>
    /// Возвращает событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события.</param>
    /// <returns>
    /// HTTP 200 (OK) с событием, если найдено; иначе — HTTP 404 (Not Found).
    /// </returns>
    [HttpGet("{id}")]
    public IActionResult GetEvent(Guid id)
    {
        var evt = _eventService.GetEvent(id);
        return evt is null ? NotFound() : Ok(evt);
    }

    /// <summary>
    /// Добавляет новое событие.
    /// </summary>
    /// <param name="evtDto">Модель события, переданная в теле запроса. Не должна быть null.</param>
    /// <returns>
    /// HTTP 201 (Created) с URL нового ресурса в заголовке <c>Location</c>, если успешно;
    /// иначе — HTTP 400 (Bad Request).
    /// </returns>
    [HttpPost]
    public IActionResult AddEvent([FromBody]EventDto evtDto)
    {
        try
        {
            Event addedEvent = _eventService.AddEvent(evtDto);
            return CreatedAtAction(nameof(GetEvent), new { id = addedEvent.Id }, addedEvent);
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Обновляет существующее событие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо обновить.</param>
    /// <param name="evtDto">Новые данные события.</param>
    /// <returns>
    /// HTTP 200 (OK) с обновлённым событием, если найдено и изменено; иначе — HTTP 404 (Not Found).
    /// </returns>
    [HttpPut("{id}")]
    public IActionResult UpdateEvent(Guid id, [FromBody]EventDto evtDto)
    {
        var updated = _eventService.UpdateEvent(id, evtDto);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>
    /// Удаляет событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события для удаления.</param>
    /// <returns>
    /// HTTP 204 (No Content), если событие успешно удалено; иначе — HTTP 404 (Not Found).
    /// </returns>
    [HttpDelete("{id}")]
    public IActionResult RemoveEvent(Guid id)
    {
        var result = _eventService.RemoveEvent(id);
        return result ? NoContent() : NotFound();
    }
}