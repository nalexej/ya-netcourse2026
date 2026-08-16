using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace EventMgtApi.Infrastructure.Services;

/// <summary>
/// Реализация IJwtTokenService — генерирует JWT-токен с подписью.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _settings;

    public JwtTokenService(IOptions<JwtOptions> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Login),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return token;
    }
}
