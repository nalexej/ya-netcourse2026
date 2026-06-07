using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventMgtApi.Tests;

public class ConcurrentTests
{
        [Fact]
        public async Task Event_TryReserveSeats_ConcurrentRequests_RespectsSeatLimit()
        {
            // Arrange
            const int totalSeats = 5;
            const int concurrentRequests = 20;

            var @event = TestDataFactory.CreateTestEvent(totalSeats: totalSeats);
            var successfulReservations = new List<int>();

            var lockObj = new object();

            // Act
            var tasks = Enumerable.Range(1, concurrentRequests)
                .Select(i => Task.Run(() =>
                {
                    if (@event.TryReserveSeats(1))
                    {
                        lock (lockObj)
                        {
                            successfulReservations.Add(i);
                        }
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert
            successfulReservations.Count.Should().Be(totalSeats,
                $"Должно быть ровно {totalSeats} успешных резервирований, но получено {successfulReservations.Count}.");

            @event.AvailableSeats.Should().Be(0,
                "Должно быть 0 доступных мест после всех резервирований.");
        }

    [Fact]
    public async Task Event_ReleaseSeats_Concurrent_ReleaseDoesNotExceedTotal()
    {
        // Arrange
        const int totalSeats = 5;
        var @event = TestDataFactory.CreateTestEvent(totalSeats: totalSeats);

        // Занять все места через TryReserveSeats (гарантирует атомарность)
        for (int i = 0; i < totalSeats; i++)
        {
            @event.TryReserveSeats().Should().BeTrue();
        }

        // Act: конкурентный возврат 10 мест (но всего было 5)
        var tasks = Enumerable.Range(1, 10)
            .Select(_ => Task.Run(() => @event.ReleaseSeats(1)))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        @event.AvailableSeats.Should().Be(totalSeats,
            "Доступных мест не должно превышать TotalSeats.");
    }

    [Fact]
    public async Task Booking_Creation_IsUniqueUnderConcurrency()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        const int concurrentRequests = 10;

        var bookings = new List<Booking>();
        var lockObj = new object();

        // Act: 10 конкурентных созданий броней
        var tasks = Enumerable.Range(1, concurrentRequests)
            .Select(_ => Task.Run(() =>
            {
                var booking = TestDataFactory.CreateBooking(eventId);
                lock (lockObj)
                {
                    bookings.Add(booking);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        bookings.Count.Should().Be(concurrentRequests,
            $"Должно быть создано {concurrentRequests} броней, но получено {bookings.Count}.");

        // Проверяем уникальность Id
        var ids = bookings.Select(b => b.Id).ToList();
        ids.Distinct().Count().Should().Be(concurrentRequests,
            $"Все брони должны иметь уникальные Id, но найдено дубликатов. Всего: {ids.Count}, уникальных: {ids.Distinct().Count()}.");

        // Дополнительно: проверяем, что все брони относятся к одному событию
        bookings.All(b => b.EventId == eventId).Should().BeTrue(
            "Все брони должны относиться к одному событию.");
    }


    // тест на уникальность Id при 10 одновременных вызовах CreateBookingAsync через BookingService
    [Fact]
    public async Task CreateBookingAsync_IdsAreUniqueUnderConcurrency()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventRepoMock = new Mock<IEventRepository>();
        var bookingRepoMock = new Mock<IBookingRepository>();

        var @event = TestDataFactory.CreateTestEvent(totalSeats: 10);
        eventRepoMock.Setup(r => r.GetById(eventId))
            .Returns(@event);

        // Мок обновления
        eventRepoMock.Setup(r => r.Update(It.IsAny<Event>()))
            .Returns(() =>
            {
                // Нужно имитировать уменьшение AvailableSeats — см. ниже
                return true;
            });

        // Мок добавления брони
        bookingRepoMock.Setup(r => r.Add(It.IsAny<Booking>()));

        var service =new BookingService(bookingRepoMock.Object, eventRepoMock.Object);

        var successfulIds = new List<Guid>();
        var lockObj = new object();

        // Act: 10 конкурентных вызовов CreateBookingAsync
        var tasks = Enumerable.Range(1, 10)
            .Select(_ => Task.Run(async () =>
            {
                var result = await service.CreateBookingAsync(eventId);
                lock (lockObj)
                {
                    successfulIds.Add(result.Id);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert: все Id уникальны
        Assert.Equal(10, successfulIds.Count);
        successfulIds.Distinct().Count().Should().Be(10,
            "Все 10 вызовов CreateBookingAsync должны вернуть уникальные Id.");
    }

}
