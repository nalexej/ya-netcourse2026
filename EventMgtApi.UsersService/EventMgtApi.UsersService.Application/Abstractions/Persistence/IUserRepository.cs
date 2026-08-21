using EventMgtApi.UsersService.Domain.Entities;

namespace EventMgtApi.UsersService.Application.Abstractions.Persistence;

/// <summary>
/// Интерфейс репозитория для работы с пользователями.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Асинхронно возвращает пользователя по логину.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пользователь, если найден; иначе <see langword="null"/>.</returns>
    Task<User?> GetByLoginAsync(string login, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно возвращает пользователя по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пользователь, если найден; иначе <see langword="null"/>.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно добавляет нового пользователя.
    /// </summary>
    /// <param name="user">Пользователь для добавления.</param>
    /// <param name="ct">Токен отмены.</param>
    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно сохраняет изменения в базе данных.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    Task SaveChangesAsync(CancellationToken ct = default);
}
