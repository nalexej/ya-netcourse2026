using Confluent.Kafka;
using EventMgtApi.BookingsService.Application.Persistence;
using EventMgtApi.BookingsService.Domain.Entities;
using EventMgtApi.Contracts.Enums;
using EventMgtApi.Contracts.Options;
using EventMgtApi.Contracts.ServiceInteraction;
using EventMgtApi.Contracts.ServiceInteraction.ServiceEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventMgtApi.BookingsService.Infrastructure.BackgroundServices;

public class BookingServiceMessagingConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingServiceMessagingConsumer> _logger;
    private readonly KafkaOptions _options;

    public BookingServiceMessagingConsumer(
        IServiceProvider serviceProvider,
        ILogger<BookingServiceMessagingConsumer> logger,
        IOptions<KafkaOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var topics = _options.Topics;

        if (topics == null || topics.Length == 0)
        {
            _logger.LogWarning("Список топиков для consumer пуст — ничего не подписываем.");
            return;
        }

        const int maxRetries = 10;
        const int retryDelayMs = 3000;

        IConsumer<string, string> consumer = null!;
        Exception lastError = null!;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                consumer = new ConsumerBuilder<string, string>(config).Build();
                consumer.Subscribe(topics);

                _logger.LogInformation(
                    "Kafka Consumer запущен успешно. Топики: {Topics}, Группа: {Group}",
                    string.Join(", ", topics), _options.ConsumerGroup);
                break;
            }
            catch (ConsumeException ex)
            {
                lastError = ex;
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(
                        "Не удалось подписаться на топики (попытка {Attempt}/{Max}). Ошибка: {Error}. Повтор через {Delay}мс.",
                        attempt, maxRetries, ex.Error.Reason, retryDelayMs);
                    await Task.Delay(retryDelayMs, stoppingToken);
                }
            }
        }

        if (consumer == null)
        {
            _logger.LogError(lastError, "Не удалось запустить Kafka Consumer после {Max} попыток", maxRetries);
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    if (consumeResult?.Message != null)
                    {
                        await ProcessMessageAsync(consumeResult, stoppingToken);
                        consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Ошибка при чтении сообщения из Kafka");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> message, CancellationToken ct)
    {
        _logger.LogDebug(
            "Получено сообщение из топика {Topic}, partition {Partition}, offset {Offset}",
            message.Topic, message.Partition, message.Offset);

        using var scope = _serviceProvider.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        object? deserialized;
        try
        {
            deserialized = message.Topic switch
            {
                ServiceInteractionConstants.BookingConfirmationFailedTopic =>
                    System.Text.Json.JsonSerializer.Deserialize(message.Message.Value, typeof(BookingConfirmationFailed)),
                _ => null
            };

            if (deserialized == null)
            {
                _logger.LogWarning("Не удалось десериализовать сообщение в топике {Topic}", message.Topic);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка десериализации сообщения в топике {Topic}", message.Topic);
            return;
        }

        switch (deserialized)
        {
            case BookingConfirmationFailed failed:
                await ProcessBookingFailedAsync(failed, bookingRepository, ct);
                break;

            default:
                _logger.LogWarning("Неизвестный тип события в топике {Topic}", message.Topic);
                break;
        }
    }

    private async Task ProcessBookingFailedAsync(
        BookingConfirmationFailed failed,
        IBookingRepository bookingRepository,
        CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(failed.BookingId, ct);
        if (booking == null)
        {
            _logger.LogError("Бронь {BookingId} не найдена", failed.BookingId);
            return;
        }

        if (booking.Status is BookingStatus.Pending or BookingStatus.Confirmed)
        {
            booking.Reject();
            await bookingRepository.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Бронь {BookingId} отклонена — недостаточно мест для события {EventId}",
                failed.BookingId, failed.EventId);
        }
    }
}