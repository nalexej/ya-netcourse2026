using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventMgtApi.Contracts.ServiceInteraction;
using EventMgtApi.EventsService.Domain.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventMgtApi.EventsService.Infrastructure.ServiceInteractions;

public class KafkaTopicInitializer : IHostedService
{
    private readonly IOptions<KafkaConsumerOptions> _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(
        IOptions<KafkaConsumerOptions> options,
        ILogger<KafkaTopicInitializer> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _options.Value.BootstrapServers
            };

            using var adminClient = new AdminClientBuilder(config).Build();

            var topicName = ServiceInteractionConstants.BookingConfirmedTopic;

            // Проверяем, существует ли топик
            var metadata = adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(5));
            var topic = metadata.Topics.FirstOrDefault(t => t.Topic == topicName);

            if (topic != null && topic.Partitions.Count > 0)
            {
                _logger.LogInformation("Топик '{Topic}' уже существует.", topicName);
                return Task.CompletedTask;
            }

            // Создаём топик
            adminClient.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }
            }).Wait(ct);

            _logger.LogInformation("Топик '{Topic}' создан.", topicName);
        }
        catch (Exception ex)
        {
            // Не валю запуск — топик может быть создан позже или уже существующий
            _logger.LogWarning(ex, "Не удалось инициализировать топик '{Topic}'. Это не критично.", ServiceInteractionConstants.BookingConfirmedTopic);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}