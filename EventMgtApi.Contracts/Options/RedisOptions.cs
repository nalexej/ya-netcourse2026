namespace EventMgtApi.Contracts.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = "localhost:6379";
    public int ConnectTimeout { get; set; } = 5000;
    public bool AbortOnConnectFail { get; set; } = false;
    public int ConnectRetry { get; set; } = 1;
}
