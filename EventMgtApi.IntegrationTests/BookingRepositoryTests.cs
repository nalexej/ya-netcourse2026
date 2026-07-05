using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Interfaces;
using EventMgtApi.Infrastructure.DataAccess;
using EventMgtApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Testcontainers.PostgreSql;
using FluentAssertions;

namespace EventMgtApi.IntegrationTests;

public class BookingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventapi")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    private Event CreateTestEvent(AppDbContext context, string title = "Test Event")
    {
        var @event = Event.Create(
            title: title,
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            totalSeats: 100,
            description: "Test");

        context.Events.Add(@event);
        context.SaveChanges();
        return @event;
    }

    [Fact]
    public async Task AddAsync_AddsBooking_ToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var bookingRepo = new BookingRepository(context);
        var @event = CreateTestEvent(context);
        var booking = new Booking(@event.Id);

        // Act
        await bookingRepo.AddAsync(booking, CancellationToken.None);
        await bookingRepo.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var savedBooking = await verifyCtx.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentNullException_ForNullBooking()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.AddAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllBookings()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        var @event = CreateTestEvent(context);
        var bookings = new List<Booking>
        {
            new Booking(@event.Id),
            new Booking(@event.Id),
            new Booking(@event.Id)
        };

        foreach (var b in bookings) await repo.AddAsync(b, CancellationToken.None);
        await repo.SaveChangesAsync();

        // Act
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(context);
        var result = await verifyRepo.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetByEventIdAsync_ReturnsBookings_ForExistingEvent()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        var @event1 = CreateTestEvent(context, "Event 1");
        var @event2 = CreateTestEvent(context, "Event 2");

        var bookings = new[]
        {
            new Booking(@event1.Id),
            new Booking(@event1.Id),
            new Booking(@event2.Id)
        };

        foreach (var b in bookings) await repo.AddAsync(b, CancellationToken.None);
        await repo.SaveChangesAsync();

        // Act
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(verifyCtx);
        var result = await verifyRepo.GetByEventIdAsync(@event1.Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal(@event1.Id, b.EventId));
    }

    [Fact]
    public async Task GetByEventIdAsync_ReturnsEmpty_ForEmptyGuidId()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        // Act
        var result = await repo.GetByEventIdAsync(Guid.Empty);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsOnlyPendingBookings()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        var @event = CreateTestEvent(context);
        var bookings = new[]
        {
            new Booking(@event.Id){Status = BookingStatus.Pending},
            new Booking(@event.Id){Status = BookingStatus.Pending},
            new Booking(@event.Id){Status = BookingStatus.Confirmed},
        };

        foreach (var b in bookings) await repo.AddAsync(b, CancellationToken.None);
        await repo.SaveChangesAsync();

        // Act
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(verifyCtx);
        var result = await verifyRepo.GetByStatusAsync(BookingStatus.Pending);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }

    [Fact]
    public async Task GetPendingBookingsIdsAsync_ReturnsOnlyPendingIds()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        var @event = CreateTestEvent(context);
        var bookings = new[]
        {
            new Booking(@event.Id){Status = BookingStatus.Pending},
            new Booking(@event.Id){Status = BookingStatus.Pending},
            new Booking(@event.Id){Status = BookingStatus.Confirmed},
            new Booking(@event.Id){Status = BookingStatus.Pending},
            new Booking(@event.Id){Status = BookingStatus.Rejected}
        };

        foreach (var b in bookings) await repo.AddAsync(b, CancellationToken.None);
        await repo.SaveChangesAsync();

        // Act
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(verifyCtx);
        var result = await verifyRepo.GetPendingBookingsIdsAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task SaveChangesAsync_SavesBookingUpdates()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        var @event = CreateTestEvent(context);
        var booking = new Booking(@event.Id) { Status = BookingStatus.Pending };
        await repo.AddAsync(booking, CancellationToken.None);
        await repo.SaveChangesAsync();

        // Act — обновляем через новый контекст
        await using var updateCtx = CreateContext();
        var updateRepo = new BookingRepository(updateCtx);
        var toUpdate = await updateRepo.GetByIdAsync(booking.Id);
        Assert.NotNull(toUpdate);
        toUpdate.Status = BookingStatus.Rejected;

        await updateRepo.SaveChangesAsync();

        // Assert
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(verifyCtx);
        var updated = await verifyRepo.GetByIdAsync(booking.Id);
        Assert.Equal(BookingStatus.Rejected, updated!.Status);
    }
}