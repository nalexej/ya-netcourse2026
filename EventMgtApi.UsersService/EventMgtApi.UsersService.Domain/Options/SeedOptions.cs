namespace EventMgtApi.UsersService.Domain.Options;

/// <summary>
/// Параметры начального заполнения базы данных.
/// </summary>
public class SeedOptions
{
    public List<AdminUser> Admins { get; set; } = new();

    public class AdminUser
    {
        public string Login { get; set; } = string.Empty;
        public string? Password { get; set; }
    }
}
