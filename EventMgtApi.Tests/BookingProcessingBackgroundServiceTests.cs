using EventMgtApi.Application.DTOs;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Interfaces;
using EventMgtApi.Infrastructure.BackgroundServices;
using EventMgtApi.Infrastructure.Repositories;
using EventMgtApi.Tests;
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
            var @event = TestDataFactory.CreateTestEvent(totalSeats: 5);
            var booking = TestDataFactory.CreateBooking(@event.Id, BookingStatus.Pending);

            var repoMock = new Mock<IBookingRepository>();
            var eventMock = new Mock<IEventRepository>();
            var loggerMock = new Mock<ILogger<BookingProcessingBackgroundService>>();

            // Модифицируем состояние брони через UpdateAsync
            repoMock.Setup(r => r.GetByStatusAsync(BookingStatus.Pending))
                .ReturnsAsync(() => new[] { booking }.Where(b => b.Status == BookingStatus.Pending).ToArray());

            eventMock.Setup(r => r.GetByIdAsync(@event.Id))
                .ReturnsAsync(@event); // вернёт валидное событие

            repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
                .Callback<Booking>(updated =>
                {
                    // Имитируем сохранение: обновляем объект
                    booking.Status = updated.Status;
                    booking.ProcessedAt = updated.ProcessedAt;
                })
                .ReturnsAsync(true);

            var service = new BookingProcessingBackgroundService(repoMock.Object, eventMock.Object, loggerMock.Object);

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
        public async Task BackgroundService_MissingEvent_ReturnRejectBookings()
        {
            // Arrange
            var booking = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Pending);

            var repoMock = new Mock<IBookingRepository>();
            var eventMock = new Mock<IEventRepository>();
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

            var service = new BookingProcessingBackgroundService(repoMock.Object, eventMock.Object, loggerMock.Object);

            // Act
            await service.StartAsync(CancellationToken.None);
            await Task.Delay(7000); // 7 сек
            await service.StopAsync(CancellationToken.None);

            // Assert
            booking.Status.Should().Be(BookingStatus.Rejected);
            booking.ProcessedAt.Should().NotBeNull();
            repoMock.Verify(r => r.UpdateAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task BackgroundService_WhenStoppingTokenIsCancelled_StopsExecution()
        {
            // Arrange
            var repoMock = new Mock<IBookingRepository>();
            var eventMock = new Mock<IEventRepository>();
            var logger = NullLogger<BookingProcessingBackgroundService>.Instance;

            // Возвращаем брони, чтобы вызвать задержку (имитация работы)
            var booking = TestDataFactory.CreateBooking(Guid.NewGuid(), BookingStatus.Pending);

            repoMock.Setup(r => r.GetByStatusAsync(BookingStatus.Pending))
                .ReturnsAsync(new[] { booking });

            // Успешное обновление
            repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync(true);

            var service = new BookingProcessingBackgroundService(repoMock.Object, eventMock.Object, logger);

            var cts = new CancellationTokenSource();

            // Act
            await service.StartAsync(cts.Token); // передаём наш токен

            // Отменяем => должно прервать ExecuteAsync
            cts.Cancel();

            // Ждём завершения (с таймаутом)
            await Task.Delay(700);

            // Assert
            // Не должно быть исключений
            // (если сервис не падает — значит, корректно обработал отмену)
        }

        [Fact]
        public async Task BackgroundService_WhenUpdateBookingFails_ReleaseSeats()
        {
            var @event = TestDataFactory.CreateTestEvent(totalSeats: 1);
            var booking = TestDataFactory.CreateBooking(@event.Id, BookingStatus.Pending);

            var eventRepoMock = new Mock<IEventRepository>();
            var bookingRepoMock = new Mock<IBookingRepository>();

            // 1. GetByIdAsync — успех
            eventRepoMock
                .Setup(r => r.GetByIdAsync(@event.Id))
                .ReturnsAsync(@event);

            // 2. GetByStatusAsync — вернёмPending бронь
            int getStatusCallCount = 0;
            bookingRepoMock
                .Setup(r => r.GetByStatusAsync(BookingStatus.Pending))
                .Callback(() => getStatusCallCount++) //отладка
                .ReturnsAsync(new[] { booking });

            bookingRepoMock
                .SetupSequence(r => r.UpdateAsync(It.IsAny<Booking>()))
                .ThrowsAsync(new InvalidOperationException()) // 1-й вызов — ошибка
                .ReturnsAsync(true);  // 2-й вызов — успех

            //UpdateAsync(@event) — вызывается** только в catch**, 1 раз
            Event? savedEvent = null;
            eventRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Event>()))
                .Callback<Event>(e => savedEvent = e)
                .ReturnsAsync(true);

            var service = new BookingProcessingBackgroundService(
                bookingRepoMock.Object, eventRepoMock.Object, NullLogger<BookingProcessingBackgroundService>.Instance);

            // Act
            var cts = new CancellationTokenSource();
            await service.StartAsync(cts.Token);
            await Task.Delay(3000, cts.Token); // достаточно для обработки
            cts.Cancel();
            await Task.Delay(700);

            // Assert
            //Assert.Equal(1, getStatusCallCount);
            Assert.NotNull(savedEvent);
            Assert.Equal(1, savedEvent!.AvailableSeats); // место восстановлено!
        }

        [Theory]
        [InlineData(0, BookingStatus.Rejected)]
        [InlineData(1, BookingStatus.Confirmed)]
        [InlineData(2, BookingStatus.Confirmed)]
        public async Task BackgroundService_NewBookingAfterReject_ResultDependsOnAvailableSeats(int availableSeats, BookingStatus expectedStatus)
        {
            // Arrange
            var totalSeats = Math.Max(1, availableSeats); // TotalSeats должен быть ≥ 1
            var @event = TestDataFactory.CreateTestEvent(totalSeats: totalSeats, availableSeats: availableSeats); 
            var bookingNew = TestDataFactory.CreateBooking(@event.Id, BookingStatus.Pending);

            var eventRepoMock = new Mock<IEventRepository>();
            var bookingRepoMock = new Mock<IBookingRepository>();

            bookingRepoMock
                .Setup(r => r.GetByStatusAsync(BookingStatus.Pending))
                .ReturnsAsync(new[] { bookingNew });

            eventRepoMock
                .Setup(r => r.GetByIdAsync(@event.Id))
                .ReturnsAsync(@event);

            int callCount = 0;
            bookingRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Booking>()))
                .Callback(() => callCount++)
                .Returns((Booking b) => callCount == 1
                    ? (availableSeats > 0 ? Task.FromResult(true)
                                          : Task.FromException<bool>(new InvalidOperationException()))
                    : Task.FromResult(true));

            int updateEventCallCount = 0;
            eventRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Event>()))
                .Callback(() => updateEventCallCount++)
                .ReturnsAsync(true);

            var service = new BookingProcessingBackgroundService(
                bookingRepoMock.Object, eventRepoMock.Object, NullLogger<BookingProcessingBackgroundService>.Instance);

            // Act
            var cts = new CancellationTokenSource();
            await service.StartAsync(cts.Token);
            await Task.Delay(2500, cts.Token);
            cts.Cancel();
            await Task.Delay(100);

            // Assert
            bookingNew.Status.Should().Be(expectedStatus,
                $"при {availableSeats} доступных местах бронь должна быть {expectedStatus}");
        }

        [Theory]
        [InlineData(1)] // 1 место — старая и новая бронь конкурируют
        public async Task BookingProcessing_WhenOldBookingRejected_NewBookingCanUseTheSameSeat(int totalSeats)
        {
            // Arrange
            var @event = TestDataFactory.CreateTestEvent(totalSeats);
            var bookingOld = TestDataFactory.CreateBooking(@event.Id, BookingStatus.Pending);
            var bookingNew = TestDataFactory.CreateBooking(@event.Id, BookingStatus.Pending);

            var eventRepoMock = new Mock<IEventRepository>();
            var bookingRepoMock = new Mock<IBookingRepository>();

            // 1. GetByStatusAsync — возвращает обеPending брони
            bookingRepoMock
                .Setup(r => r.GetByStatusAsync(BookingStatus.Pending))
                .ReturnsAsync(new[] { bookingOld, bookingNew });

            // 2. GetByIdAsync — возвращает событие
            eventRepoMock
                .Setup(r => r.GetByIdAsync(@event.Id))
                .ReturnsAsync(@event);

            // 3. UpdateAsync(bookingOld) — Confirm → ошибка (первый вызов)
            int callCountOld = 0;
            bookingRepoMock
                .Setup(r => r.UpdateAsync(bookingOld))
                .Callback(() => callCountOld++)
                .Returns((Booking b) => callCountOld == 1
                    ? Task.FromException<bool>(new InvalidOperationException())
                    : Task.FromResult(true));

            // 4. UpdateAsync(bookingNew) — Confirm → успех (потому что место освободилось)
            bookingRepoMock
                .SetupSequence(r => r.UpdateAsync(bookingNew))
                .ReturnsAsync(true) // Confirm
                .ReturnsAsync(true); // Reject

            // 5. UpdateAsync(@event) — один вызов (в catch для bookingOld)
            int updateEventCallCount = 0;
            eventRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Event>()))
                .Callback(() => updateEventCallCount++)
                .ReturnsAsync(true);

            var service = new BookingProcessingBackgroundService(
                bookingRepoMock.Object, eventRepoMock.Object, NullLogger<BookingProcessingBackgroundService>.Instance);

            // Act
            var cts = new CancellationTokenSource();
            await service.StartAsync(cts.Token);
            await Task.Delay(2500, cts.Token);
            cts.Cancel();
            await Task.Delay(100);

            // Assert
            bookingOld.Status.Should().Be(BookingStatus.Rejected,
                "старая бронь должна быть отклонена после ошибки.");

            bookingNew.Status.Should().Be(BookingStatus.Confirmed,
                "новая бронь должна быть подтверждена, так как место освободилось.");

            updateEventCallCount.Should().Be(1, "ReleaseSeats() должен быть вызван один раз.");

            @event.AvailableSeats.Should().Be(totalSeats,
                $"после Confirm(bookingNew) место НЕ должно быть занято (ReserveSeat() не вызывается). Actual: {@event.AvailableSeats}");
        }
    }
}