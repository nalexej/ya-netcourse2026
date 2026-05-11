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
    /// <returns>
    /// Новый экземпляр <see cref="List{T}"/>, содержащий копию всех текущих событий.
    /// Изменения возвращаемого списка не влияют на внутреннее состояние сервиса.
    /// </returns>
    List<Event> GetEvents();

    /// <summary>
    /// Возвращает событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события для поиска.</param>
    /// <returns>
    /// Найденное событие, если оно существует; иначе — <see langword="null"/>.
    /// </returns>
    Event? GetEvent(Guid id);

    /// <summary>
    /// Добавляет новое событие в коллекцию.
    /// </summary>
    /// <param name="evtDto">Событие, которое необходимо добавить. Не должно быть <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Вызывается, если параметр <paramref name="evtDto"/> равен <see langword="null"/>.
    /// </exception>
    Event AddEvent(EventDto evtDto);

    /// <summary>
    /// Обновляет существующее событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо обновить.</param>
    /// <param name="evtDto">Новые данные события. Не должны быть <see langword="null"/>.</param>
    /// <returns>
    /// Возвращает обновлённое событие, если оно найдено и успешно изменено; иначе — <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Вызывается, если параметр <paramref name="evtDto"/> равен <see langword="null"/>.
    /// </exception>
    Event? UpdateEvent(Guid id, EventDto evtDto);

    /// <summary>
    /// Удаляет событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо удалить.</param>
    /// <returns>
    /// <see langword="true"/>, если событие было найдено и удалено; иначе — <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Предполагается, что идентификаторы уникальны. 
    /// Если несколько событий имеют одинаковый <paramref name="id"/> (что маловероятно), все они будут удалены.
    /// </remarks>
    bool RemoveEvent(Guid id);
}