using Confluent.Kafka;
using EventMgtApi.Contracts.Events;
using EventMgtApi.Contracts.ServiceInteraction;
using EventMgtApi.Contracts.ServiceInteraction.ServiceEvents;
using EventMgtApi.EventsService.Application.Persistence;
using EventMgtApi.EventsService.Domain.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventMgtApi.EventsService.Infrastructure.ServiceInteractions;

public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private readonly KafkaConsumerOptions _options;

    public KafkaConsumerBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<KafkaConsumerBackgroundService> logger,
        IOptions<KafkaConsumerOptions> options)
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
            // Автокоммит OFF — коммитим вручную после обработки
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(ServiceInteractionConstants.BookingConfirmedTopic);

        _logger.LogInformation(
            "Kafka Consumer запущен. Топик: {Topic}, Группа: {Group}",
            ServiceInteractionConstants.BookingConfirmedTopic, _options.ConsumerGroup);

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
                        // Коммитим только после успешной обработки
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

        // Создаём scope, чтобы получить scoped сервисы (репозиторий, DbContext)
        using var scope = _serviceProvider.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        BookingConfirmed? bookingConfirmed;
        try
        {
            bookingConfirmed = System.Text.Json.JsonSerializer.Deserialize<BookingConfirmed>(message.Message.Value);
            if (bookingConfirmed == null)
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

        // Ищем событие
        var @event = await eventRepository.GetByIdAsync(bookingConfirmed.EventId, ct);
        if (@event == null)
        {
            _logger.LogWarning(
                "Событие {EventId} не найдено — пропускаем сообщение.",
                bookingConfirmed.EventId);
            return;
        }

        // Пытаемся зарезервировать места
        if (!@event.TryReserveSeats(bookingConfirmed.SeatsCount))
        {
            _logger.LogWarning(
                "Недостаточно мест для события {EventId} — пропускаем сообщение. " +
                "Доступно: {Available}, нужно: {Needed}",
                bookingConfirmed.EventId, @event.AvailableSeats, bookingConfirmed.SeatsCount);
            return;
        }

        // Сохраняем изменения
        await eventRepository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Зарезервировано {Seats} мест для события {EventId} по брони {BookingId}",
            bookingConfirmed.SeatsCount, bookingConfirmed.EventId, bookingConfirmed.BookingId);
    }
}