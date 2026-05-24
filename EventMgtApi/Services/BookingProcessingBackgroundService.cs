using EventMgtApi.Models;
using EventMgtApi.Repositories;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Фоновый сервис, периодически обрабатывающий брони со статусом Pending.
/// </summary>
public class BookingProcessingBackgroundService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5); // Период опроса хранилища

    /// <summary>
    /// Инициализирует новый экземпляр сервиса с указанным репозиторием бронирований.
    /// </summary>
    /// <param name="bookingRepository">Репозиторий для доступа к данным о бронях.</param>
    public BookingProcessingBackgroundService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    /// <summary>
    /// Основной цикл фоновой службы.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Обрабатываем все ожидающие брони
            await ProcessPendingBookingsAsync(stoppingToken);

            // Ждём перед следующей итерацией (или прерываемся при отмене)
            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Получает брони в статусе Pending и подтверждает их с задержкой.
    /// </summary>
    private async Task ProcessPendingBookingsAsync(CancellationToken stoppingToken)
    {
        // Получаем все брони, ожидающие обработки
        var pendingBookings = await _bookingRepository.GetByStatusAsync(BookingStatus.Pending);

        foreach (var booking in pendingBookings)
        {
            // Имитация длительной внешней операции (например, интеграция с платёжной системой)
            await Task.Delay(2000, stoppingToken);

            // Подтверждаем бронь
            booking.Status = BookingStatus.Confirmed;
            booking.ProcessedAt = DateTime.UtcNow;

            // Сохраняем обновлённое состояние
            await _bookingRepository.UpdateAsync(booking);
        }
    }
}