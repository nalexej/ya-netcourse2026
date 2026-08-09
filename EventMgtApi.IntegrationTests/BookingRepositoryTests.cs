using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Infrastructure.Persistence;
using EventMgtApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventMgtApi.IntegrationTests;

[Collection("Database")]
public class BookingRepositoryTests 
{
    private readonly PostgreSqlContainer _postgres;

    public BookingRepositoryTests(PostgreSqlContainerFixture fixture)
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

    private async Task<Guid> CreateTestEvent(AppDbContext context, string title = "Test Event")
    {
        var @event = Event.Create(
            title: title,
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            totalSeats: 100,
            description: "Test");

        context.Events.Add(@event);
        context.SaveChanges();
        return @event.Id;
    }

    private async Task<Guid> CreateTestUserAsync(AppDbContext context, UserRole userRole = UserRole.User)
    {
        var @user = User.Create("testuser_concurrent", "dummyhash", userRole);
        context.Users.Add(@user);
        await context.SaveChangesAsync();
        return @user.Id;
    }

    [Fact]
    public async Task AddAsync_AddsBooking_ToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var bookingRepo = new BookingRepository(context);
        var eventId = await CreateTestEvent(context);
        var userId = await CreateTestUserAsync(context);

        var booking = new Booking(eventId, userId);

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
    public async Task GetByIdAsync_ReturnsBooking_ById()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        var eventId = await CreateTestEvent(context);
        var userId = await CreateTestUserAsync(context);

        var booking = new Booking(eventId, userId);

        await repo.AddAsync(booking, CancellationToken.None);
        await repo.SaveChangesAsync();

        // Act — получаем через новый контекст
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(verifyCtx);
        var result = await verifyRepo.GetByIdAsync(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result!.Id);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForNonExistentId()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        var nonExistentId = Guid.NewGuid();

        // Act
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(verifyCtx);
        var result = await verifyRepo.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllBookings()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repo = new BookingRepository(context);

        var eventId = await CreateTestEvent(context);
        var userId = await CreateTestUserAsync(context);

        var bookings = new List<Booking>
        {
            new Booking(@eventId, userId),
            new Booking(@eventId, userId),
            new Booking(@eventId, userId),
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

        var event1Id = await CreateTestEvent(context, "Event 1");
        var event2Id = await CreateTestEvent(context, "Event 2");
        var userId = await CreateTestUserAsync(context);

        var bookings = new[]
        {
            new Booking(event1Id, userId),
            new Booking(event1Id, userId),
            new Booking(event2Id, userId)
        };

        foreach (var b in bookings) await repo.AddAsync(b, CancellationToken.None);
        await repo.SaveChangesAsync();

        // Act
        await using var verifyCtx = CreateContext();
        var verifyRepo = new BookingRepository(verifyCtx);
        var result = await verifyRepo.GetByEventIdAsync(event1Id);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal(event1Id, b.EventId));
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

        var eventId = await CreateTestEvent(context);
        var userId = await CreateTestUserAsync(context);

        var bookings = new[]
        {
            new Booking(eventId, userId){Status = BookingStatus.Pending},
            new Booking(eventId, userId){Status = BookingStatus.Pending},
            new Booking(eventId, userId){Status = BookingStatus.Confirmed},
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

        var eventId = await CreateTestEvent(context);
        var userId = await CreateTestUserAsync(context);

        var bookings = new[]
        {
            new Booking(eventId, userId){Status = BookingStatus.Pending},
            new Booking(eventId, userId){Status = BookingStatus.Pending},
            new Booking(eventId, userId){Status = BookingStatus.Confirmed},
            new Booking(eventId, userId){Status = BookingStatus.Pending},
            new Booking(eventId, userId){Status = BookingStatus.Rejected}
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

        var eventId = await CreateTestEvent(context);
        var userId = await CreateTestUserAsync(context);

        var booking = new Booking(eventId, userId) { Status = BookingStatus.Pending };
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