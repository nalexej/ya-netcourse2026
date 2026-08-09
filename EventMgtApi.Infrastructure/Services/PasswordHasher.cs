using EventMgtApi.Application.Abstractions.Services;
using System.Security.Cryptography;
using System.Text;

namespace EventMgtApi.Infrastructure.Services;

/// <summary>
/// Реализация IPasswordHasher на базе SHA-256.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        var computedHash = HashPassword(password);
        return computedHash == hash;
    }
}
