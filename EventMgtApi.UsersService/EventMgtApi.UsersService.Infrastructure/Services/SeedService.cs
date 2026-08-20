using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Contracts.Enums;
using EventMgtApi.UsersService.Application.Abstractions.Persistence;
using EventMgtApi.UsersService.Application.Abstractions.Services;
using EventMgtApi.UsersService.Domain.Entities;
using EventMgtApi.UsersService.Domain.Options;
using EventMgtApi.UsersService.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Options;

namespace EventMgtApi.UsersService.Infrastructure.Services;

/// <summary>
/// Реализация seed-сервиса для создания начальных данных.
/// </summary>
public sealed class SeedService : ISeedService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SeedOptions _options;

    private readonly string AnonymousLogin = "anonymous";
    private readonly Guid AnonymousUserId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    public SeedService(IUserRepository userRepository, IPasswordHasher passwordHasher, IOptions<SeedOptions> options)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task SeedAsync()
    {
        foreach (var admin in _options.Admins)
        {
            var existing = await _userRepository.GetByLoginAsync(admin.Login);
            if (existing is not null)
                continue;

            var password = admin.Password
                ?? throw new InvalidOperationException($"Пароль для администратора '{admin.Login}' не указан в конфигурации.");

            var hash = _passwordHasher.HashPassword(password);
            var user = User.Create(admin.Login, hash, UserRole.Admin);

            await _userRepository.AddAsync(user);
        }

        // Добавляем анонимного пользователя
        var isAnonymousUserExists = await _userRepository.GetByLoginAsync(AnonymousLogin);
        if (isAnonymousUserExists is null)
        {
            var anonymousUser = User.Create(
                    login: "anonymous",
                    passwordHash: Guid.NewGuid().ToString(), // dummy hash
                    role: UserRole.User
            );
            anonymousUser.Id = AnonymousUserId;
            await _userRepository.AddAsync(anonymousUser);
        }

        await _userRepository.SaveChangesAsync();
    }
}
