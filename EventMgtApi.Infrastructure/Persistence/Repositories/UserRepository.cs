using EventMgtApi.Application.Abstractions.Persistence.Repositories;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.Infrastructure.Repositories;

/// <summary>
/// Реализация <see cref="IUserRepository"/> для хранения пользователей в PostgreSQL.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByLoginAsync(string login, CancellationToken ct = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Login == login, ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <inheritdoc />
    public Task AddAsync(User user, CancellationToken ct = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        return _context.Users.AddAsync(user, ct).AsTask();
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
    }
}
