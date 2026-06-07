using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
        var @event = TestDataFactory.CreateTestEvent(totalSeats: 5);

        // Сначала "занимаем" все места (имитация)
        @event.AvailableSeats = 0;

        var successfulReleases = new List<int>();
        var lockObj = new object();

        // 10 конкурентных вызовов ReleaseSeats(1)
        var tasks = Enumerable.Range(1, 10)
            .Select(i => Task.Run(() =>
            {
                @event.ReleaseSeats(1);
                lock (lockObj)
                {
                    successfulReleases.Add(i);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        successfulReleases.Count.Should().Be(10,
            "Все 10 вызовов ReleaseSeats должны выполниться (даже если это приведёт к AvailableSeats > TotalSeats, но Math.Min защитит).");

        @event.AvailableSeats.Should().Be(5, "AvailableSeats не может превышать TotalSeats.");
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

}
