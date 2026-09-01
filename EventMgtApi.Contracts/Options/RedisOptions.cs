namespace EventMgtApi.Contracts.Options;

public sealed class RedisOptions
{
     public const string SectionName = "Redis";
     public string ConnectionString { get; set; } = "localhost:6379";
}
