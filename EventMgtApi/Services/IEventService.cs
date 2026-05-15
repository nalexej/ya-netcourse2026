using EventMgtApi.Exceptions;
using EventMgtApi.Models;
using EventMgtApi.Models.Dto;

namespace EventMgtApi.Services;

/// <summary>
/// Интерфейс для сервиса управления событиями.
/// Определяет основные операции: добавление, получение, обновление и удаление событий.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Возвращает список всех событий.
    /// </summary>
    ///<returns>
    /// Экземпляр<see cref = "PaginatedResult{T}" />, содержащий отфильтрованные и разбитые на страницы события.
    ///</returns>
    PaginatedResult<EventDtoResponse> GetEvents(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10);

    /// <summary>
    /// Возвращает событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события для поиска.</param>
    /// <returns>
    /// Копия найденного события в виде <see cref="EventDtoResponse"/>.</returns>
    /// <exception cref="NotFoundException">
    /// Исключение выбрасывается, если событие с указанным <paramref name="id"/> не найдено.
    /// </exception>   
    EventDtoResponse GetEvent(Guid id);

    /// <summary>
    /// Добавляет новое событие.
    /// </summary>
    /// <param name="evtDto">Данные события, которое необходимо добавить. Не должно быть null.</param>
    /// <returns>Возвращает копию добавленного события в виде <see cref="EventDtoResponse"/>.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если параметр <paramref name="evtDto"/> равен null.</exception>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если:
    /// <list type="bullet">
    ///   <item><description>Заголовок пуст или состоит только из пробелов.</description></item>
    ///   <item><description>Дата начала не меньше даты окончания.</description></item>
    /// </list>
    /// </exception>
    EventDtoResponse AddEvent(EventDto evtDto);

    /// <summary>
    /// Обновляет существующее событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор события для обновления.</param>
    /// <param name="evtDto">Новые данные события. Не должен быть <see langword="null"/>.</param>
    /// <returns>Возвращает обновлённую копию события в виде <see cref="EventDtoResponse"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если параметр <paramref name="evtDto"/> равен <see langword="null"/>.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если:
    /// <list type="bullet">
    ///   <item><description>Заголовок пуст или состоит только из пробелов.</description></item>
    ///   <item><description>Дата начала не меньше даты окончания.</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="NotFoundException">
    /// Выбрасывается, если событие с указанным <paramref name="id"/> не найдено.
    /// </exception>
    EventDtoResponse UpdateEvent(Guid id, EventDto evtDto);

    /// <summary>
    /// Удаляет событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо удалить.</param>
    /// <exception cref="NotFoundException">
    /// Выбрасывается, если событие с указанным <paramref name="id"/> не найдено.
    /// </exception>
    void RemoveEvent(Guid id);
}