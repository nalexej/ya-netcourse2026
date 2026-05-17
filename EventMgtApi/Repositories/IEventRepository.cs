using EventMgtApi.Models;

namespace EventMgtApi.Repositories
{
    /// <summary>
    /// Интерфейс для доступа к данным событий.
    /// Определяет основные операции: получение, добавление, обновление, удаление.
    /// </summary>
    public interface IEventRepository
    {
        /// <summary>
        /// Возвращает все события.
        /// </summary>
        /// <returns>Список всех событий.</returns>
        List<Event> GetAll();

        /// <summary>
        /// Возвращает событие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор события.</param>
        /// <returns>Событие, если найдено; иначе <see langword="null"/>.</returns>
        Event? GetById(Guid id);

        /// <summary>
        /// Добавляет новое событие.
        /// </summary>
        /// <param name="event">Событие для добавления. Не должно быть null.</param>
        void Add(Event @event);

        /// <summary>
        /// Обновляет существующее событие.
        /// </summary>
        /// <param name="event">Событие с обновлёнными данными. Должно иметь существующий Id.</param>
        /// <returns><see langword="true"/>, если обновление прошло успешно; иначе <see langword="false"/>.</returns>
        bool Update(Event @event);

        /// <summary>
        /// Удаляет событие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого события.</param>
        /// <returns><see langword="true"/>, если событие удалено; иначе <see langword="false"/>.</returns>
        bool Delete(Guid id);

        /// <summary>
        /// Возвращает общее количество событий.
        /// </summary>
        /// <returns>Число событий в хранилище.</returns>
        int Count();
    }
}