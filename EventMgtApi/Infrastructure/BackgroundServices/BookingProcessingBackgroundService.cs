using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventMgtApi.Infrastructure.BackgroundServices;

/// <summary>
/// Фоновый сервис, периодически обрабатывающий брони со статусом Pending.
/// Выполняет автоматическое подтверждение броней с задержкой.
/// Сервис корректно реагирует на отмену и логирует жизненный цикл.
/// </summary>
public class BookingProcessingBackgroundService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _processingDelay = TimeSpan.FromSeconds(2);
    private readonly int _seatsToReleaseOnReject = 1;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса с указанным репозиторием и логгером.
    /// </summary>
    /// <param name="bookingRepository">
    /// Репозиторий для доступа к данным о бронях. Не должен быть null.
    /// </param>
    /// <param name="eventRepository">
    /// Репозиторий для доступа к данным о событиях. Не должен быть null.
    /// </param>
    /// <param name="logger">
    /// Служба логирования. Используется для записи информации о работе фонового процесса.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="bookingRepository"/> или <paramref name="logger"/> равны null.
    /// </exception>
    public BookingProcessingBackgroundService(
        IBookingRepository bookingRepository,
        IEventRepository eventRepository,
        ILogger<BookingProcessingBackgroundService> logger)
    {
        _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
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
    /// Асинхронно обрабатывает одну бронь со статусом Pending.
    /// - Имитация внешнего вызова (Task.Delay) выполняется до захвата семафора.
    /// - Критическая секция (обновление брони и хранилища) защищена семафором.
    /// - Если событие не найдено — бронь отклоняется, место возвращается в пул.
    /// - При неожиданной ошибке — бронь отклоняется, место возвращается, обновляются оба хранилища.
    /// </summary>
    /// <param name="booking">Бронь для обработки.</param>
    /// <param name="stoppingToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию обработки.</returns>
    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        // Имитация внешнего вызова выполняется ДО захвата семафора
        try
        {
            await Task.Delay(_processingDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Задержка обработки брони {BookingId} прервана отменой.", booking.Id);
            return;
        }

        // Критическая секция: захват семафора для защиты записи в хранилище
        await _processingSemaphore.WaitAsync(stoppingToken);

        try
        {
            // Получаем событие из хранилища
            var @event = await _eventRepository.GetByIdAsync(booking.EventId);
            if (@event is null)
            {
                // Событие не найдено — просто отклоняем бронь
                booking.Reject();

                _logger.LogWarning("Событие {EventId} не найдено для брони {BookingId}. Отклоняем бронь.",
                    booking.EventId, booking.Id);

                var updated = await _bookingRepository.UpdateAsync(booking);
                _logger.LogDebug("Бронь {BookingId} отклонена (событие не найдено).", booking.Id);
                return;
            }

            // Событие найдено — подтверждаем бронь
            booking.Confirm();
            await _bookingRepository.UpdateAsync(booking);
            _logger.LogDebug("Бронь {BookingId} подтверждена.", booking.Id);

        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Обработка брони {BookingId} прервана отменой.", booking.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке брони {BookingId}", booking.Id);

            // Получаем событие
            var @event = await _eventRepository.GetByIdAsync(booking.EventId);
            if (@event is not null)
            {
                @event.ReleaseSeats(_seatsToReleaseOnReject); // возвращаем место
                await _eventRepository.UpdateAsync(@event);
            }

            booking.Reject();
            await _bookingRepository.UpdateAsync(booking);

            _logger.LogWarning("Бронь {BookingId} отклонена после ошибки. Место возвращено: {EventExists}.",
                booking.Id, @event is not null);
        }
        finally
        {
            // освобождаем семафор
            _processingSemaphore.Release();
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
            var pendingBookings = (await _bookingRepository.GetByStatusAsync(BookingStatus.Pending)).ToList();

            if (!pendingBookings.Any())
            {
                _logger.LogDebug("Нет броней в статусе Pending — пропуск обработки.");
                return;
            }

            _logger.LogInformation("Найдено {Count} ожидающих броней для обработки.", pendingBookings.Count);

            var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
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