using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.UsersService.Application.Abstractions.Services;
using EventMgtApi.Contracts.Users.DTOs;
using EventMgtApi.UsersService.Application.Abstractions.Persistence;
using EventMgtApi.UsersService.Domain.Exceptions;
using EventMgtApi.Contracts.Enums;
using EventMgtApi.UsersService.Domain.Entities;

namespace EventMgtApi.Application.Users;

/// <summary>
/// Реализация сервиса аутентификации и регистрации пользователей.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task<RegisterResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(request.Login))
            throw new ValidationException(
                new Dictionary<string, ICollection<string>> { ["Login"] = ["Логин обязателен."] });

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException(
                new Dictionary<string, ICollection<string>> { ["Password"] = ["Пароль обязателен."] });

        if (request.Password.Length < 6)
            throw new ValidationException(
                new Dictionary<string, ICollection<string>> { ["Password"] = ["Пароль должен содержать минимум 6 символов."] });

        // Проверяем, не занят ли логин
        var existingUser = await _userRepository.GetByLoginAsync(request.Login, cancellationToken);
        if (existingUser is not null)
            throw new ValidationException(
                new Dictionary<string, ICollection<string>> { ["Login"] = ["Пользователь с таким логином уже существует."] });

        // Хешируем пароль
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Создаём пользователя
        var role = UserRole.User; // Всегда User, в целях безопасности

        var user = User.Create(request.Login.Trim(), passwordHash, role);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new RegisterResponseDto
        {
            UserId = user.Id,
            Login = user.Login,
            Role = user.Role.ToString()
        };
    }

    /// <inheritdoc />
    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(request.Login))
            throw new ValidationException(
                new Dictionary<string, ICollection<string>> { ["Login"] = ["Логин обязателен."] });

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException(
                new Dictionary<string, ICollection<string>> { ["Password"] = ["Пароль обязателен."] });

        // Ищем пользователя по логину
        var user = await _userRepository.GetByLoginAsync(request.Login, cancellationToken);

        // Если пользователя нет или он системный — сразу отказываем в доступе
        if (user is null || IsSystemUser(user.Login))
            throw new InvalidCredentialsException("Неверный логин или пароль.");

        // Проверяем пароль
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException("Неверный логин или пароль.");

        // Генерируем JWT-токен
        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            Login = user.Login,
            Role = user.Role.ToString()
        };
    }

    /// <summary>
    /// Проверяет, является ли логин системным, т.е. заблокированным для входа и регистрации.
    /// </summary>
    private bool IsSystemUser(string login)
    {
        // Список зарезервированных имен
        var reservedLogins = new[] { "anonymous" }; // Добавьте нужные
        return reservedLogins.Any(reserved =>
            string.Equals(login, reserved, StringComparison.OrdinalIgnoreCase));
    }
}
