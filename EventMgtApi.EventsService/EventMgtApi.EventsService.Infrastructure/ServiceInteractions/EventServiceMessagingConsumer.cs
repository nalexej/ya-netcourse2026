using Confluent.Kafka;
using EventMgtApi.Contracts.Caching;
using EventMgtApi.Contracts.Events;
using EventMgtApi.Contracts.Options;
using EventMgtApi.Contracts.ServiceInteraction;
using EventMgtApi.Contracts.ServiceInteraction.ServiceEvents;
using EventMgtApi.EventsService.Application.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventMgtApi.EventsService.Infrastructure.ServiceInteractions;

public class EventServiceMessagingConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventServiceMessagingConsumer> _logger;
    private readonly KafkaOptions _options;
    private readonly ICacheClient _cache;

    public EventServiceMessagingConsumer(
        IServiceProvider serviceProvider,
        ILogger<EventServiceMessagingConsumer> logger,
        IOptions<KafkaOptions> options,
        ICacheClient cache)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _cache = cache;
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

        using var scope = _serviceProvider.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var processedBookingRepository = scope.ServiceProvider.GetRequiredService<IProcessedBookingRepository>();

        object? deserialized;
        try
        {
            deserialized = message.Topic switch
            {
                ServiceInteractionConstants.BookingConfirmedTopic =>
                    System.Text.Json.JsonSerializer.Deserialize(message.Message.Value, typeof(BookingConfirmed)),
                ServiceInteractionConstants.BookingCancelledTopic =>
                    System.Text.Json.JsonSerializer.Deserialize(message.Message.Value, typeof(BookingCancelled)),
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
            case BookingConfirmed confirmed:
                await ProcessBookingConfirmedAsync(confirmed, eventRepository, processedBookingRepository, ct);
                break;

            case BookingCancelled cancelled:
                await ProcessBookingCancelledAsync(cancelled, eventRepository, processedBookingRepository, ct);
                break;

            default:
                _logger.LogWarning("Неизвестный тип события в топике {Topic}", message.Topic);
                break;
        }
    }

    private async Task ProcessBookingConfirmedAsync(
        BookingConfirmed confirmed,
        IEventRepository eventRepository,
        IProcessedBookingRepository processedBookingRepository,
        CancellationToken ct)
    {
        // Идемпотентность: проверяем, не обрабатывалось ли уже это бронирование
        if (await processedBookingRepository.ExistsAsync(confirmed.EventId, confirmed.BookingId, "Confirmed", ct))
        {
            _logger.LogWarning(
                "Бронь {BookingId} для события {EventId} уже обрабатывалась — пропускаем (идемпотентность).",
                confirmed.BookingId, confirmed.EventId);
            return;
        }

        var @event = await eventRepository.GetByIdAsync(confirmed.EventId, ct);
        if (@event == null)
        {
            _logger.LogError(
                "Событие {EventId} не найдено — бронь {BookingId} не может быть обработана",
                confirmed.EventId, confirmed.BookingId);

            var eventPublisher = _serviceProvider.GetRequiredService<IEventPublisher>();

            var failedEvent = new BookingConfirmationFailed(
                bookingId: confirmed.BookingId,
                eventId: confirmed.EventId,
                userId: confirmed.UserId
            );

            await eventPublisher.PublishAsync(
                failedEvent,
                key: confirmed.EventId.ToString(),
                ct: ct
            );

            return;
        }

        // Проверка: дата начала события в прошлом
        if (@event.StartAt <= DateTime.UtcNow) 
        {
            _logger.LogWarning(
                "Нельзя забронировать событие {EventId}, дата начала которого в прошлом. Бронь {BookingId}. ",
                confirmed.EventId, confirmed.BookingId);

            var eventPublisher = _serviceProvider.GetRequiredService<IEventPublisher>();

            var failedEvent = new BookingConfirmationFailed(
                bookingId: confirmed.BookingId,
                eventId: confirmed.EventId,
                userId: confirmed.UserId
            );

            await eventPublisher.PublishAsync(
                failedEvent,
                key: confirmed.EventId.ToString(),
                ct: ct
            );

            return;
        }

        if (!@event.TryReserveSeats(confirmed.SeatsCount))
        {
            _logger.LogWarning(
                "Недостаточно мест для события {EventId} — бронь {BookingId}. " +
                "Доступно: {Available}, нужно: {Needed}",
                confirmed.EventId, confirmed.BookingId,
                @event.AvailableSeats, confirmed.SeatsCount);

            var eventPublisher = _serviceProvider.GetRequiredService<IEventPublisher>();

            var failedEvent = new BookingConfirmationFailed(
                bookingId: confirmed.BookingId,
                eventId: confirmed.EventId,
                userId: confirmed.UserId
            );

            await eventPublisher.PublishAsync(
                failedEvent,
                key: confirmed.EventId.ToString(),
                ct: ct
            );

            return;
        }

        await processedBookingRepository.AddAsync(confirmed.EventId, confirmed.BookingId, "Confirmed", ct);
        await eventRepository.SaveChangesAsync(ct);

        await _cache.RemoveAsync($"event:{confirmed.EventId}", ct);

        _logger.LogInformation(
            "Зарезервировано {Seats} мест для события {EventId} по брони {BookingId}",
            confirmed.SeatsCount, confirmed.EventId, confirmed.BookingId);
    }

    private async Task ProcessBookingCancelledAsync(
        BookingCancelled cancelled,
        IEventRepository eventRepository,
        IProcessedBookingRepository processedBookingRepository,
        CancellationToken ct)
    {
        // Идемпотентность: проверяем, не обрабатывалось ли уже это бронирование
        if (await processedBookingRepository.ExistsAsync(cancelled.EventId, cancelled.BookingId, "Cancelled", ct))
        {
            _logger.LogWarning(
                "Бронь {BookingId} для события {EventId} уже обрабатывалась (Cancelled) — пропускаем (идемпотентность).",
                cancelled.BookingId, cancelled.EventId);
            return;
        }

        var @event = await eventRepository.GetByIdAsync(cancelled.EventId, ct);
        if (@event == null)
        {
            _logger.LogWarning(
                "Событие {EventId} не найдено — пропускаем освобождение мест для брони {BookingId}",
                cancelled.EventId, cancelled.BookingId);
            return;
        }

        @event.ReleaseSeats(cancelled.SeatsCount);
        await processedBookingRepository.AddAsync(cancelled.EventId, cancelled.BookingId, "Cancelled", ct);
        await eventRepository.SaveChangesAsync(ct);
        
        await _cache.RemoveAsync($"event:{cancelled.EventId}", ct);

        _logger.LogInformation(
            "Освобождено {Seats} мест для события {EventId} по отмене брони {BookingId}",
            cancelled.SeatsCount, cancelled.EventId, cancelled.BookingId);
    }
}