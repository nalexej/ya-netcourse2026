namespace EventMgtApi.Domain.Options;

/// <summary>
/// Параметры конфигурации JWT-токенов.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Секретный ключ для подписи токена (base64 или hex строка).
    /// </summary>
    public required string Secret { get; set; }

    /// <summary>
    /// Издатель токена.
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// Аудитория токена.
    /// </summary>
    public required string Audience { get; set; }

    /// <summary>
    /// Время жизни токена в минутах.
    /// </summary>
    public int ExpiryMinutes { get; set; } = 60;
}
