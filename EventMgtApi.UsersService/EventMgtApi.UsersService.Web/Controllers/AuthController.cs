using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Contracts.Users.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventMgtApi.UsersService.Web.Controllers;

/// <summary>
/// Контроллер для аутентификации и регистрации пользователей.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="userService">Сервис аутентификации и регистрации пользователей</param>
    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Регистрация нового пользователя.
    /// </summary>
    /// <param name="request">Данные регистрации (login, password, optional role).</param>
    /// <returns>Код 201 Created с данными созданного пользователя.</returns>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        if (request == null)
            return BadRequest("Тело запроса не может быть null.");

        var result = await _userService.RegisterAsync(request);
        return Created(String.Empty, result);
    }

    /// <summary>
    /// Вход пользователя в систему.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (request == null)
            return BadRequest("Тело запроса не может быть null.");

        var result = await _userService.LoginAsync(request);
        return Ok(result);
    }
}
