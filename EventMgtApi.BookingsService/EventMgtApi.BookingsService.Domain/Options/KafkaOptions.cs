namespace EventMgtApi.BookingsService.Domain.Options;

/// <summary>
/// Параметры конфигурации Kafka.
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>
    /// Адрес брокера Kafka (bootstrap servers).
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";
}
