using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.Interfaces;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Options;
using EventMgtApi.Domain.Enums;
using Microsoft.Extensions.Options;

namespace EventMgtApi.Application.Users;

/// <summary>
/// Реализация seed-сервиса для создания начальных данных.
/// </summary>
public sealed class SeedService : ISeedService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SeedOptions _options;

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

        await _userRepository.SaveChangesAsync();
    }
}
