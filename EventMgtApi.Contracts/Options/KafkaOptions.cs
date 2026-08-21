namespace EventMgtApi.Contracts.Options;

/// <summary>
/// Параметры конфигурации Kafka
/// </summary>
public class KafkaOptions
{
    public const string SectionName = "Kafka";

    /// <summary>
    /// Адрес брокера Kafka (bootstrap servers).
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Имя группы потребителей. Сообщения внутри группы распределяются между экземплярами.
    /// </summary>
    public string ConsumerGroup { get; set; } = "events-service-group";

    /// <summary>
    /// Список имён Kafka-топиков, которые нужно инициализировать.
    /// </summary>
    public string[] Topics { get; set; } = Array.Empty<string>();
}
