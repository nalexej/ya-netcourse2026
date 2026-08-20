using Confluent.Kafka;
using EventMgtApi.Contracts.Options;
using EventMgtApi.Contracts.ServiceInteraction;
using EventMgtApi.Contracts.ServiceInteraction.ServiceEvents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventMgtApi.BookingsService.Infrastructure.ServiceInteraction;

/// <summary>
/// Издатель сообщений службы бронирования.
/// </summary>
public sealed class BookingServiceMessagingPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<BookingServiceMessagingPublisher> _logger;
    private bool _disposed;

    public BookingServiceMessagingPublisher(IOptions<KafkaOptions> kafkaOptions, ILogger<BookingServiceMessagingPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T eventMessage, string? key = null, CancellationToken ct = default) where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(BookingServiceMessagingPublisher));

        var json = System.Text.Json.JsonSerializer.Serialize(eventMessage);
        var messageKey = key ?? Guid.NewGuid().ToString();

        var message = new Message<string, string>
        {
            Key = messageKey,
            Value = json
        };

        var topic = GetTopicForMessage(eventMessage);

        var result = await _producer.ProduceAsync(topic, message, ct);

        _logger.LogDebug(
            "Сообщение опубликовано в топик {Topic}, partition {Partition}, offset {Offset}.",
            result.Topic, result.Partition, result.Offset);
    }

    private static string GetTopicForMessage<T>(T eventMessage) where T : class
    {
        return eventMessage switch
        {
            BookingConfirmed => ServiceInteractionConstants.BookingConfirmedTopic,
            BookingCancelled => ServiceInteractionConstants.BookingCancelledTopic,
            _ => throw new ArgumentException($"Неизвестный тип события: {typeof(T).Name}")
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _producer.Flush();
        _producer.Dispose();
        _disposed = true;

        _logger.LogInformation("BookingServiceMessagingPublisher остановлен и освобождён.");
    }
}
