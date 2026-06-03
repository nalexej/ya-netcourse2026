using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Interfaces;
using EventMgtApi.Infrastructure.BackgroundServices;
using EventMgtApi.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace EventMgtApiTests
{
    public class BookingProcessingBackgroundServiceTests
    {
        [Fact]
        public async Task BackgroundService_WhenStarted_ProcessesPendingBookings()
        {
            // Arrange
            var booking = new Booking(Guid.NewGuid())
            {
                Id = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var repoMock = new Mock<IBookingRepository>();
            var loggerMock = new Mock<ILogger<BookingProcessingBackgroundService>>();

            // Мы будем модифицировать состояние брони через UpdateAsync
            repoMock.Setup(r => r.GetByStatusAsync(BookingStatus.Pending))
                .ReturnsAsync(() => new[] { booking }.Where(b => b.Status == BookingStatus.Pending).ToArray());

            repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(updated =>
                {
                    // Имитируем сохранение: обновляем объект
                    booking.Status = updated.Status;
                    booking.ProcessedAt = updated.ProcessedAt;
                })
                .ReturnsAsync(true);

            var service = new BookingProcessingBackgroundService(repoMock.Object, loggerMock.Object);

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(7000); // 7 сек
            await service.StopAsync(CancellationToken.None);

            // Assert
            booking.Status.Should().Be(BookingStatus.Confirmed);
            booking.ProcessedAt.Should().NotBeNull();
            repoMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task BackgroundService_WhenStoppingTokenIsCancelled_StopsExecution()
        {
            // Arrange
            var repoMock = new Mock<IBookingRepository>();
            var logger = NullLogger<BookingProcessingBackgroundService>.Instance;

            // Возвращаем брони, чтобы вызвать задержку (имитация работы)
            var booking = new Booking(Guid.NewGuid()) { Id = Guid.NewGuid(), Status = BookingStatus.Pending };
            repoMock.Setup(r => r.GetByStatusAsync(BookingStatus.Pending))
                .ReturnsAsync(new[] { booking });

            // Успешное обновление
            repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync(true);

            var service = new BookingProcessingBackgroundService(repoMock.Object, logger);

            var cts = new CancellationTokenSource();

            // Act
            await service.StartAsync(cts.Token); // передаём наш токен

            // Отменяем => должно прервать ExecuteAsync
            cts.Cancel();

            // Ждём завершения (с таймаутом)
            await Task.Delay(700); 
            await service.StopAsync(CancellationToken.None); // корректное завершение

            // Assert
            // Не должно быть исключений
            // (если сервис не падает — значит, корректно обработал отмену)
        }
    }
}
