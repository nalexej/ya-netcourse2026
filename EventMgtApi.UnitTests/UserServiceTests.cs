using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.Interfaces;
using EventMgtApi.Application.Users;
using EventMgtApi.Application.Users.DTOs;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventMgtApi.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly IUserService _service;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();

        _service = new UserService(
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object);
    }

    // === РЕГИСТРАЦИЯ ===

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Login = "newuser@example.com",
            Password = "StrongPass123",
            Role = "User" // Строка, как ожидается в DTO
        };

        _passwordHasherMock.Setup(p => p.HashPassword(request.Password))
            .Returns("hashed_" + request.Password);

        _userRepoMock.Setup(r => r.GetByLoginAsync(request.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Настройка моков репозитория для сохранения
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, c) =>
            {
                // Имитируем присвоение ID, которое обычно делает БД
                u.Id = Guid.NewGuid();
            })
            .Returns(Task.CompletedTask);

        _userRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Login.Should().Be("newuser@example.com");
        result.UserId.Should().NotBeEmpty();
        result.Role.Should().Be("User");

        // Проверка, что метод AddAsync был вызван
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_EmptyLogin_ThrowsValidationException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Login = "",
            Password = "StrongPass123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RegisterAsync(request));

        Assert.Contains("Логин обязателен", exception?.Errors["Login"].FirstOrDefault());
    }

    [Fact]
    public async Task RegisterAsync_ShortPassword_ThrowsValidationException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Login = "user@example.com",
            Password = "123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RegisterAsync(request));

        Assert.Contains("минимум 6 символов", exception?.Errors["Password"].FirstOrDefault());
    }

    [Fact]
    public async Task RegisterAsync_DuplicateLogin_ThrowsValidationException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Login = "existing@example.com",
            Password = "StrongPass123"
        };

        var existingUser = User.Create(request.Login, "dummy_hash", UserRole.User);

        _userRepoMock.Setup(r => r.GetByLoginAsync(request.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.RegisterAsync(request));

        Assert.Contains("уже существует", exception?.Errors["Login"].FirstOrDefault());
    }

    // === АВТОРИЗАЦИЯ ===

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Login = "testuser@example.com",
            Password = "StrongPass123"
        };

        var hashedPassword = _passwordHasherMock.Setup(p => p.HashPassword(request.Password)).Returns("hashed_password");

        var user = User.Create(request.Login, "hashed_password", UserRole.User);

        _userRepoMock.Setup(r => r.GetByLoginAsync(request.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(p => p.VerifyPassword(request.Password, "hashed_password"))
            .Returns(true);

        _jwtTokenServiceMock.Setup(j => j.GenerateToken(user))
            .Returns("fake_jwt_token");

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("fake_jwt_token");
        result.Login.Should().Be(request.Login);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Login = "unknown@example.com",
            Password = "StrongPass123"
        };

        _userRepoMock.Setup(r => r.GetByLoginAsync(request.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.LoginAsync(request));

        Assert.Contains("Неверный логин или пароль", exception.Message);
    }
    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsNotFoundException()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Login = "user@example.com",
            Password = "WrongPassword"
        };

        var user = User.Create(request.Login, "correct_hash", UserRole.User);

        _userRepoMock.Setup(r => r.GetByLoginAsync(request.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(p => p.VerifyPassword(request.Password, "correct_hash"))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.LoginAsync(request));

        Assert.Contains("Неверный логин или пароль", exception.Message);
    }

}