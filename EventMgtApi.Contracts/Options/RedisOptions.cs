namespace EventMgtApi.Contracts.Options;

public sealed class RedisOptions
{
     public const string SectionName = "Redis";
     public string ConnectionString { get; set; } = "localhost:6379";
     public int EventCacheTtlSeconds { get; set; } = 300;
     public int TopEventsCacheTtlSeconds { get; set; } = 300;
}
