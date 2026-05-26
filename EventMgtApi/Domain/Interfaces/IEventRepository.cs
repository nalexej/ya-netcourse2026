using EventMgtApi.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventMgtApi.Domain.Interfaces
{
    /// <summary>
    /// Интерфейс для доступа к данным событий.
    /// Определяет асинхронные операции: получение, добавление, обновление, удаление.
    /// </summary>
    public interface IEventRepository
    {
        /// <summary>
        /// Асинхронно возвращает все события.
        /// </summary>
        /// <returns>Список всех событий.</returns>
        Task<List<Event>> GetAllAsync();

        /// <summary>
        /// Асинхронно возвращает событие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор события.</param>
        /// <returns>Событие, если найдено; иначе <see langword="null"/>.</returns>
        Task<Event?> GetByIdAsync(Guid id);

        /// <summary>
        /// Асинхронно добавляет новое событие.
        /// </summary>
        /// <param name="event">Событие для добавления. Не должно быть null.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(Event @event);

        /// <summary>
        /// Асинхронно обновляет существующее событие.
        /// </summary>
        /// <param name="event">Событие с обновлёнными данными. Должно иметь существующий Id.</param>
        /// <returns><see langword="true"/>, если обновление прошло успешно; иначе <see langword="false"/>.</returns>
        Task<bool> UpdateAsync(Event @event);

        /// <summary>
        /// Асинхронно удаляет событие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого события.</param>
        /// <returns><see langword="true"/>, если событие удалено; иначе <see langword="false"/>.</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Асинхронно возвращает общее количество событий.
        /// </summary>
        /// <returns>Число событий в хранилище.</returns>
        Task<int> CountAsync();
    }
}