
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventMgtApi.Contracts.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventMgtApi.EventsService.Infrastructure.ServiceInteractions;

public class KafkaTopicInitializer : IHostedService
{
    private readonly IOptions<KafkaOptions> _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaTopicInitializer> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _options.Value.BootstrapServers
            };

            using var adminClient = new AdminClientBuilder(config).Build();

            var topics = _options.Value.Topics;

            if (topics == null || topics.Length == 0)
            {
                _logger.LogWarning("Список топиков для инициализации пуст — пропускаем.");
                return;
            }

            foreach (var topicName in topics)
            {
                await InitializeTopicAsync(adminClient, topicName, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось инициализировать Kafka-топики. Это не критично.");
        }
    }

    private async Task InitializeTopicAsync(
        IAdminClient adminClient,
        string topicName,
        CancellationToken ct)
    {
        var existingTopics = adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(5))
            .Topics
            .Select(t => t.Topic)
            .ToList();

        if (existingTopics.Contains(topicName))
        {
            _logger.LogInformation("Топик '{Topic}' уже существует.", topicName);
            return;
        }
        // Создаём топик
        await adminClient.CreateTopicsAsync(new[]
        {
                new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }
        });
        _logger.LogInformation("Топик '{Topic}' создан.", topicName);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}