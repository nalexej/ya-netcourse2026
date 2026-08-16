using EventMgtApi.Application.DTOs;
using EventMgtApi.Domain.Exceptions;

namespace EventMgtApi.Application.Abstractions.Services;

/// <summary>
/// Интерфейс для сервиса управления событиями.
/// Определяет асинхронные операции: получение, добавление, обновление и удаление событий.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Асинхронно возвращает список всех событий с пагинацией и фильтрацией.
    /// </summary>
    /// <param name="title">Фильтр по заголовку (опционально).</param>
    /// <param name="from">Фильтр: события начиная с этой даты (опционально).</param>
    /// <param name="to">Фильтр: события до этой даты (опционально).</param>
    /// <param name="page">Номер страницы (по умолчанию 1).</param>
    /// <param name="pageSize">Размер страницы (по умолчанию 10).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// Экземпляр <see cref="PaginatedResult{T}" />, содержащий отфильтрованные и разбитые на страницы события.
    /// </returns>
    Task<PaginatedResult<EventDtoResponse>> GetEventsAsync(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно возвращает событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события для поиска.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// Копия найденного события в виде <see cref="EventDtoResponse"/>.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Исключение выбрасывается, если событие с указанным <paramref name="id"/> не найдено.
    /// </exception>   
    Task<EventDtoResponse> GetEventAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно добавляет новое событие.
    /// </summary>
    /// <param name="evtDto">Данные события, которое необходимо добавить. Не должно быть null.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Возвращает копию добавленного события в виде <see cref="EventDtoResponse"/>.</returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если параметр <paramref name="evtDto"/> равен null.</exception>
    /// <exception cref="ValidationException">
    /// Выбрасывается, если:
    /// <list type="bullet">
    ///   <item><description>Заголовок пуст или состоит только из пробелов.</description></item>
    ///   <item><description>Дата начала не меньше даты окончания.</description></item>
    /// </list>
    /// </exception>
    Task<EventDtoResponse> AddEventAsync(EventDto evtDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно обновляет существующее событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Уникальный идентификатор события для обновления.</param>
    /// <param name="evtDto">Новые данные события. Не должен быть <see langword="null"/>.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
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
    Task<EventDtoResponse> UpdateEventAsync(Guid id, EventDto evtDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Асинхронно удаляет событие по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор события, которое необходимо удалить.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию удаления.</returns>
    /// <exception cref="NotFoundException">
    /// Выбрасывается, если событие с указанным <paramref name="id"/> не найдено.
    /// </exception>
    Task<bool> RemoveEventAsync(Guid id, CancellationToken cancellationToken = default);
}