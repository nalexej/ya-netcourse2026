using EventMgtApi.Application.Abstractions.Services;
using EventMgtApi.Application.Interfaces;
using EventMgtApi.Application.Services;
using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Domain.Exceptions;
using EventMgtApi.Domain.Options;
using EventMgtApi.Infrastructure.Persistence;
using EventMgtApi.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Testcontainers.PostgreSql;

namespace EventMgtApi.IntegrationTests;

[Collection("Database")]
public class ConcurrentTests
{
    private readonly PostgreSqlContainer _postgres;

    public ConcurrentTests(PostgreSqlContainerFixture fixture)
    {
        _postgres = fixture.PostgreSql;
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    private IServiceScope CreateScope()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddSingleton<IOptions<BookingOptions>>(
            new OptionsWrapper<BookingOptions>(new BookingOptions { MaxActiveBookings = 10 }));

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.CreateScope();
    }

    private async Task<Guid> CreateTestEventAsync(int totalSeats = 10)
    {
        await using var context = CreateContext();
        var @event = Event.Create(
            title: "Concurrent Test Event",
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            totalSeats: totalSeats,
            description: "Test");

        context.Events.Add(@event);
        await context.SaveChangesAsync();

        return @event.Id;
    }

    private async Task<Guid> CreateTestUserAsync(UserRole userRole = UserRole.User)
    {
        await using var context = CreateContext();
        var @user = User.Create("testuser_concurrent", "dummyhash", userRole);
        context.Users.Add(@user);
        await context.SaveChangesAsync();
        return @user.Id;
    }

    [Fact]
    public async Task ReserveSeats_ConcurrentRequests_RespectsSeatLimit()
    {
        // Arrange
        await ResetDatabaseAsync();

        var eventId = await CreateTestEventAsync(totalSeats: 5);
        var userId = await CreateTestUserAsync();

        const int concurrentRequests = 20;

        var successfulReservations = new ConcurrentBag<int>();
        var lockObj = new object();

        // Act: 20 concurrent запросов на резервирование по 1 месту
        var tasks = Enumerable.Range(1, concurrentRequests)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    // Создаём новый scope для каждого запроса — чтобы каждый получил свой DbContext
                    using var scope = CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                    await bookingService.CreateBookingAsync(eventId, userId);
                    lock (lockObj)
                    {
                        successfulReservations.Add(i);
                    }
                }
                catch (NoAvailableSeatsException)
                {
                    // Недостаточно мест — это нормально
                }
                catch (NotFoundException)
                {
                    // Событие не найдено — ошибка, но в данном случае не должно случиться
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Concurrency conflict — также нормально
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        successfulReservations.Count.Should().Be(5,
            $"Должно быть ровно 5 успешных резервирований, но получено {successfulReservations.Count}.");

        // Проверяем актуальное состояние через новый контекст
        await using var verifyCtx = CreateContext();
        var @event = await verifyCtx.Events.FindAsync(eventId);
        @event!.AvailableSeats.Should().Be(0,
            "Должно быть 0 доступных мест после всех резервирований.");
    }

    [Fact]
    public async Task Booking_Creation_IsUniqueUnderConcurrency()
    {
        await ResetDatabaseAsync();

        var userId = await CreateTestUserAsync();
        var eventId = await CreateTestEventAsync();

        var totalBookings = 10;

        var tasks = Enumerable.Range(1, totalBookings)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = CreateScope();
                var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var booking = new Booking(eventId, userId);
                await bookingRepo.AddAsync(booking, CancellationToken.None);
                await bookingRepo.SaveChangesAsync();
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert: проверяем в БД
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(verifyCtx);
        var savedBookings = await verifyRepo.GetAllAsync();

        savedBookings.Should().HaveCount(totalBookings,
            $"Должно быть {totalBookings} броней, но получено {savedBookings.Count()}");

        var ids = savedBookings.Select(b => b.Id).ToList();
        ids.Distinct().Should().HaveCount(totalBookings,
            $"Все брони должны иметь уникальные Id, но найдено {ids.Count - ids.Distinct().Count()} дубликатов.");

        savedBookings.All(b => b.EventId == eventId).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_DoesNotOverbookEvent()
    {
        await ResetDatabaseAsync();

        const int totalSeats = 5;
        const int concurrentRequests = 20;

        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);
        var userId = await CreateTestUserAsync();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    await bookingService.CreateBookingAsync(eventId, userId);
                    return true;
                }
                catch (NoAvailableSeatsException)
                {
                    return false;
                }
            }));

        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r);
        Assert.Equal(totalSeats, successCount);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequests_AllSuccessfulBookingsHaveUniqueIds()
    {

        await ResetDatabaseAsync();

        const int totalSeats = 10;
        const int concurrentRequests = 10;
        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);
        var userId = await CreateTestUserAsync();

        var bookingIds = new System.Collections.Concurrent.ConcurrentBag<Guid>();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var booking = await bookingService.CreateBookingAsync(eventId, userId);
                bookingIds.Add(booking.Id);
            }));

        await Task.WhenAll(tasks);

        Assert.Equal(totalSeats, bookingIds.Distinct().Count());
    }

    [Fact]
    public async Task ReserveSeats_Overbooking_ThrowsValidationException()
    {
        await ResetDatabaseAsync();

        const int totalSeats = 2;
        var eventId = await CreateTestEventAsync(totalSeats: totalSeats);
        var userId = await CreateTestUserAsync();

        // Занять все места
        for (int i = 0; i < totalSeats; i++)
        {
            using var scope = CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            await bookingService.CreateBookingAsync(eventId, userId);
        }

        // Проверить, что мест нет
        await using var verifyCtx = CreateContext();
        var verifiedEvent = await verifyCtx.Events.FindAsync(eventId);
        verifiedEvent!.AvailableSeats.Should().Be(0, "все места должны быть заняты");

        // Попытаться занять ещё 1 — должно выбросить ValidationException
        using var failScope = CreateScope();
        var failBookingService = failScope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            failBookingService.CreateBookingAsync(eventId, userId));
    }
}