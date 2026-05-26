using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Фоновый сервис, периодически обрабатывающий брони со статусом Pending.
/// Выполняет автоматическое подтверждение броней с задержкой.
/// Сервис корректно реагирует на отмену и логирует жизненный цикл.
/// </summary>
public class BookingProcessingBackgroundService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Инициализирует новый экземпляр сервиса с указанным репозиторием и логгером.
    /// </summary>
    /// <param name="bookingRepository">
    /// Репозиторий для доступа к данным о бронях. Не должен быть null.
    /// </param>
    /// <param name="logger">
    /// Служба логирования. Используется для записи информации о работе фонового процесса.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="bookingRepository"/> или <paramref name="logger"/> равны null.
    /// </exception>
    public BookingProcessingBackgroundService(
        IBookingRepository bookingRepository,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var pendingBookings = await _bookingRepository.GetByStatusAsync(BookingStatus.Pending);

            if (!pendingBookings.Any())
            {
                _logger.LogDebug("Нет броней в статусе Pending — пропуск обработки.");
                return;
            }

            _logger.LogInformation("Найдено {Count} ожидающих броней для обработки.", pendingBookings.Count());

            foreach (var booking in pendingBookings)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                try
                {
                    await Task.Delay(2000, stoppingToken);

                    booking.Status = BookingStatus.Confirmed;
                    booking.ProcessedAt = DateTime.UtcNow;

                    var updated = await _bookingRepository.UpdateAsync(booking);
                    if (!updated)
                        _logger.LogWarning("Не удалось обновить бронь {BookingId}", booking.Id);
                    else
                        _logger.LogDebug("Бронь {BookingId} подтверждена.", booking.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке брони {BookingId}", booking.Id);
                }
            }

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