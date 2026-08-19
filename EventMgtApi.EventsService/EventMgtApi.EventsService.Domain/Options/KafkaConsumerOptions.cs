namespace EventMgtApi.EventsService.Domain.Options;

/// <summary>
/// Параметры конфигурации Kafka для подписчика.
/// </summary>
public class KafkaConsumerOptions
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
}
