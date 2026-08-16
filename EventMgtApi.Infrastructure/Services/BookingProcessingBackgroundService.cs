using EventMgtApi.Application.Interfaces;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventMgtApi.Infrastructure.BackgroundServices;

/// <summary>
/// Фоновый сервис, периодически обрабатывающий брони со статусом Pending.
/// Выполняет автоматическое подтверждение броней с задержкой.
/// Сервис корректно реагирует на отмену и логирует жизненный цикл.
/// </summary>
public class BookingProcessingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _processingDelay = TimeSpan.FromSeconds(2);
    private readonly int _seatsToReleaseOnReject = 1;

    /// <summary>
    /// Инициализирует новый экземпляр фонового сервиса.
    /// </summary>
    /// <param name="scopeFactory">
    /// Фвбрика scope.
    /// </param>
    /// <param name="logger">
    /// Служба логирования. Используется для записи информации о работе фонового процесса.
    /// </param>
    public BookingProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Асинхронный метод, представляющий основной цикл фоновой службы.
    /// </summary>
    /// <param name="stoppingToken">
    /// Токен, сигнализирующий о запросе остановки приложения.
    /// Используется для корректного завершения фоновой задачи.
    /// </param>
    /// <returns>
    /// Задача, представляющая выполнение фонового сервиса.
    /// </returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновый сервис обработки броней запущен.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessPendingBookingsAsync(stoppingToken);

                try
                {
                    await Task.Delay(_pollingInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Ожидание прервано корректной отменой — выходим из цикла
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка в фоновом сервисе");
            throw;
        }
        finally
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Фоновый сервис обработки броней остановлен.");
            }
        }
    }

    /// <summary>
    /// Асинхронно обрабатывает одну бронь со статусом Pending.
    /// - Имитация внешнего вызова (Task.Delay) выполняется до захвата семафора.
    /// - Критическая секция (обновление брони и хранилища) защищена семафором.
    /// - Если событие не найдено — бронь отклоняется, место возвращается в пул.
    /// - При неожиданной ошибке — бронь отклоняется, место возвращается, обновляются оба хранилища.
    /// </summary>
    /// <param name="bookingId">Идентификатор брони для обработки.</param>
    /// <param name="stoppingToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию обработки.</returns>
    protected internal async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {

        // Имитация внешнего вызова выполняется ДО захвата семафора
        try
        {
            await Task.Delay(_processingDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Задержка обработки брони {BookingId} прервана отменой.", bookingId);
            return;
        }

        try
        {
            // Получаем событие из хранилища
            using var scope = _scopeFactory.CreateScope();

            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
            if (booking == null || booking.Status != BookingStatus.Pending)
                return;

            var @event = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);
            if (@event is null)
            {
                // Событие не найдено — просто отклоняем бронь
                booking!.Reject();
                await bookingRepository.SaveChangesAsync(stoppingToken);

                _logger.LogWarning("Событие {EventId} не найдено для брони {BookingId}. Отклоняем бронь.",
                    booking.EventId, booking.Id);

                _logger.LogDebug("Бронь {BookingId} отклонена (событие не найдено).", booking.Id);
                return;
            }

            // Событие найдено — подтверждаем бронь
            booking!.Confirm();
            await bookingRepository.SaveChangesAsync(stoppingToken);
            _logger.LogDebug("Бронь {BookingId} подтверждена.", booking.Id);

        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Обработка брони {BookingId} прервана отменой.", bookingId);
        }
        catch (Exception ex)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                // перечитываем на всякий случай
                var bkg = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);

                if (bkg != null)
                {
                    bkg.Reject();
                    var evt = await eventRepository.GetByIdAsync(bkg.EventId, stoppingToken);

                    if (evt is not null)
                    {
                        evt.ReleaseSeats(_seatsToReleaseOnReject); // возвращаем место
                        await eventRepository.SaveChangesAsync(stoppingToken);
                    }
                }
                _logger.LogError(ex, "Бронь {BookingId} отклонена в ходе обработки возникшей ошибки.", bookingId);
            }
            catch (Exception releaseEx)
            {
                _logger.LogError(releaseEx, "Не удалось отклонить Бронь {BookingId} в ходе обработки возникшей ошибки", bookingId);
            }

        }
    }

    /// <summary>
    /// Асинхронно обрабатывает все брони со статусом <see cref="BookingStatus.Pending"/>.
    /// Для каждой брони:
    /// - Имитируется внешняя обработка (задержка 2 сек).
    /// - Статус меняется на <see cref="BookingStatus.Confirmed"/>.
    /// - Устанавливается <see cref="Booking.ProcessedAt"/>.
    /// - Обновлённая бронь сохраняется в репозитории.
    /// </summary>
    /// <param name="stoppingToken">
    /// Токен отмены. Если активирован, обработка прерывается.
    /// </param>
    /// <returns>
    /// Задача, представляющая асинхронную операцию обработки.
    /// </returns>
    private async Task ProcessPendingBookingsAsync(CancellationToken stoppingToken)
    {
        try
        {
            IEnumerable<Guid> pendingBookingsIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                pendingBookingsIds = await bookingRepository.GetPendingBookingsIdsAsync(stoppingToken);

            }
            if (!pendingBookingsIds.Any())
            {
                _logger.LogDebug("Нет броней в статусе Pending — пропуск обработки.");
                return;
            }
            _logger.LogInformation("Найдено {Count} ожидающих броней для обработки.", pendingBookingsIds.Count());

            var tasks = pendingBookingsIds.Select(bookingId => ProcessBookingAsync(bookingId, stoppingToken));

            await Task.WhenAll(tasks);
            _logger.LogInformation("Обработка ожидающих броней завершена.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Обработка броней прервана отменой.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении броней в статусе Pending");
        }
    }
}