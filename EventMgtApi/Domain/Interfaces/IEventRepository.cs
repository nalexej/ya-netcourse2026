using EventMgtApi.Application.DTOs;
using EventMgtApi.Domain.Entities;

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
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Список всех событий.</returns>
        Task<IEnumerable<Event>> GetAllAsync(CancellationToken ct = default);

        /// <summary>
        /// Асинхронно возвращает список всех событий с пагинацией и фильтрацией.
        /// </summary>
        /// <param name="title">Фильтр по заголовку (опционально).</param>
        /// <param name="from">Фильтр: события начиная с этой даты (опционально).</param>
        /// <param name="to">Фильтр: события до этой даты (опционально).</param>
        /// <param name="page">Номер страницы (по умолчанию 1).</param>
        /// <param name="pageSize">Размер страницы (по умолчанию 10).</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>
        /// Экземпляр <see cref="PaginatedResult{T}" />, содержащий отфильтрованные и разбитые на страницы события.
        /// </returns>
        Task<PaginatedResult<Event>> GetFilteredPagesAsync(
            string? title,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize,
            CancellationToken ct = default);

        /// <summary>
        /// Асинхронно возвращает событие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор события.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Событие, если найдено; иначе <see langword="null"/>.</returns>
        Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Асинхронно добавляет новое событие.
        /// </summary>
        /// <param name="event">Событие для добавления. Не должно быть null.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task AddAsync(Event @event, CancellationToken ct = default);

        /// <summary>
        /// Асинхронно удаляет событие по идентификатору.
        /// </summary>
        /// <param name="event">Удаляемое событие.</param>
        /// <param name="ct">Токен отмены.</param>
        Task DeleteAsync(Event @event, CancellationToken ct = default);

        /// <summary>
        /// Асинхронно сохраняет изменения в базу данных.
        /// </summary>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Задача, представляющая операцию сохранения.</returns>
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}