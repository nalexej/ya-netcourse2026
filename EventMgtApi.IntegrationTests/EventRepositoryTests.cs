using EventMgtApi.Domain.Entities;
using EventMgtApi.Infrastructure.Persistence;
using EventMgtApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventMgtApi.IntegrationTests;

[Collection("Database")]
public class EventRepositoryTests
{
    private readonly PostgreSqlContainer _postgres;

    public EventRepositoryTests(PostgreSqlContainerFixture fixture)
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

    [Fact]
    public async Task AddAsync_AddsEvent_ToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        var @event = Event.Create(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            totalSeats: 100,
            description: "Test Description");

        // Act
        await repository.AddAsync(@event);
        await repository.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var savedEvent = await verifyContext.Events.FindAsync(@event.Id);
        Assert.NotNull(savedEvent);
        Assert.Equal(@event.Title, savedEvent!.Title);
        Assert.Equal(@event.Description, savedEvent.Description);
        Assert.Equal(@event.TotalSeats, savedEvent.TotalSeats);
        Assert.Equal(@event.AvailableSeats, savedEvent.TotalSeats);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentNullException_ForNullEvent()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_RemovesEvent_FromDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        var @event = Event.Create(
            title: "Event to Delete",
            description: "Test",
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            totalSeats: 50);

        await repository.AddAsync(@event);
        await repository.SaveChangesAsync();

        // Act — удаляем через НОВЫЙ контекст и репозиторий
        await using var deleteContext = CreateContext();
        var deleteRepo = new EventRepository(deleteContext);
        var eventToDelete = await deleteRepo.GetByIdAsync(@event.Id);
        Assert.NotNull(eventToDelete);
        await deleteRepo.DeleteAsync(eventToDelete!);
        await deleteRepo.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var verifyRepo = new EventRepository(verifyContext);
        var deletedEvent = await verifyRepo.GetByIdAsync(@event.Id);
        Assert.Null(deletedEvent);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsArgumentNullException_ForNullEvent()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.DeleteAsync(null!));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEvents()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        var events = new List<Event>
        {
            Event.Create(title: "Event 1", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 100, description: "Test1"),
            Event.Create(title: "Event 2", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 5, description: "Test2"),
            Event.Create(title: "Event 3", DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2), 6, description: "Test3"),
        };

        foreach (var @event in events)
        {
            await repository.AddAsync(@event);
        }
        await repository.SaveChangesAsync();

        // Act — получаем через НОВЫЙ контекст
        await using var verifyContext = CreateContext();
        var result = await verifyContext.Events.ToListAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, e => e.Title == "Event 1");
        Assert.Contains(result, e => e.Title == "Event 2");
        Assert.Contains(result, e => e.Title == "Event 3");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEvent_ByExistingId()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        var @event = Event.Create(
            title: "Found Event",
            description: "Test",
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            totalSeats: 100);

        await repository.AddAsync(@event);
        await repository.SaveChangesAsync();

        // Act — ВАЖНО: вызываем repository.GetByIdAsync() в новом контексте
        await using var verifyContext = CreateContext();
        var verifyRepo = new EventRepository(verifyContext);
        var result = await verifyRepo.GetByIdAsync(@event.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(@event.Id, result!.Id);
        Assert.Equal(@event.Title, result.Title);
        Assert.Equal(@event.Description, result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForNonExistentId()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        var nonExistentId = Guid.NewGuid();

        // Act — используем НОВЫЙ контекст и репозиторий
        await using var verifyContext = CreateContext();
        var verifyRepo = new EventRepository(verifyContext);
        var result = await verifyRepo.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredPagesAsync_ReturnsPagedResult_WithFilters()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        // Создаём события с разными датами и названиями
        var events = new List<Event>
        {
            Event.Create("Summer Concert", DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(1).AddDays(2), 100, description: "Rock"),
            Event.Create(title: "Winter Festival", DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(14), 50, description: "Folk"),
            Event.Create(title: "Summer Party", DateTime.UtcNow.AddMonths(2).AddDays(5), DateTime.UtcNow.AddMonths(2).AddDays(7), 200, description: "House"),
            Event.Create(title: "Spring Show", DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(15).AddDays(1), 75, description: "Classical")
        };

        foreach (var @event in events)
        {
            await repository.AddAsync(@event);
        }
        await repository.SaveChangesAsync();

        // Act — используем НОВЫЙ контекст и репозиторий
        await using var verifyContext = CreateContext();
        var verifyRepo = new EventRepository(verifyContext);

        // 1. Фильтр по названию (без учёта регистра)
        var filteredByTitle = await verifyRepo.GetFilteredPagesAsync("summer", null, null, page: 1, pageSize: 10);

        // Assert: фильтр по названию
        Assert.Equal(2, filteredByTitle.TotalCount);
        Assert.Equal(2, filteredByTitle.Items.Count);
        Assert.All(filteredByTitle.Items, e => e.Title.ToLower().Contains("summer"));
        Assert.Equal(1, filteredByTitle.Page);
        Assert.Equal(10, filteredByTitle.PageSize);

        // 2. Фильтр по дате "от" (от 0.5 мес. вперёд)
        var filteredByFrom = await verifyRepo.GetFilteredPagesAsync(null, DateTime.UtcNow.AddDays(15), null, page: 1, pageSize: 10);

        // Assert: фильтр по дате "от"
        Assert.Equal(2, filteredByFrom.TotalCount); // "Summer Concert", "Summer Party"
        Assert.Equal(2, filteredByFrom.Items.Count);

        // 3. Фильтр по дате "до" (до 0.5 мес. вперёд)
        var filteredByTo = await verifyRepo.GetFilteredPagesAsync(null, null, DateTime.UtcNow.AddDays(15), page: 1, pageSize: 10);

        // Assert: фильтр по дате "до"
        Assert.Equal(1, filteredByTo.TotalCount); // только "Winter Festival"
        Assert.Single(filteredByTo.Items);
        Assert.Equal("Winter Festival", filteredByTo.Items[0].Title);

        // 4. Комбинированный фильтр: title = "summer" AND from = 1 мес. вперёд
        var combinedFilter = await verifyRepo.GetFilteredPagesAsync("summer", DateTime.UtcNow.AddMonths(1), null, page: 1, pageSize: 10);

        // Assert: комбинированный фильтр
        Assert.Equal(1, combinedFilter.TotalCount); // "Summer Concert"

        // 5. Пагинация: страница 1, размер 2
        var page1_size2 = await verifyRepo.GetFilteredPagesAsync(null, null, null, page: 1, pageSize: 2);
        Assert.Equal(4, page1_size2.TotalCount);
        Assert.Equal(2, page1_size2.Items.Count);
        Assert.Equal(1, page1_size2.Page);
        Assert.Equal(2, page1_size2.PageSize);

        // 6. Пагинация: страница 2, размер 2 (оставшиеся 2)
        var page2_size2 = await verifyRepo.GetFilteredPagesAsync(null, null, null, page: 2, pageSize: 2);
        Assert.Equal(4, page2_size2.TotalCount);
        Assert.Equal(2, page2_size2.Items.Count);
        Assert.Equal(2, page2_size2.Page);
        Assert.Equal(2, page2_size2.PageSize);

        // 7. Пагинация: страница 3, размер 2 (пусто, только 4 события)
        var page3_size2 = await verifyRepo.GetFilteredPagesAsync(null, null, null, page: 3, pageSize: 2);
        Assert.Equal(4, page3_size2.TotalCount);
        Assert.Empty(page3_size2.Items);
        Assert.Equal(3, page3_size2.Page);
        Assert.Equal(2, page3_size2.PageSize);

        // 8. Пагинация: страница 1, размер 10 (все 4 сразу)
        var page1_size10 = await verifyRepo.GetFilteredPagesAsync(null, null, null, page: 1, pageSize: 10);
        Assert.Equal(4, page1_size10.TotalCount);
        Assert.Equal(4, page1_size10.Items.Count);
        Assert.Equal(1, page1_size10.Page);
        Assert.Equal(10, page1_size10.PageSize);
    }

    [Fact]
    public async Task SaveChangesAsync_SavesChanges()
    {
        // Arrange
        await ResetDatabaseAsync();
        var context = CreateContext();
        var repository = new EventRepository(context);

        var @event = Event.Create(
            title: "Updated Event",
            description: "Initial Description",
            startAt: DateTime.UtcNow.AddHours(1),
            endAt: DateTime.UtcNow.AddHours(2),
            totalSeats: 100);

        await repository.AddAsync(@event);
        await repository.SaveChangesAsync();

        // Изменяем событие
        await using var updateContext = CreateContext();
        var updateRepo = new EventRepository(updateContext);
        var eventToUpdate = await updateRepo.GetByIdAsync(@event.Id);
        Assert.NotNull(eventToUpdate);
        eventToUpdate.Description = "Updated Description";

        // Act
        await updateRepo.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        var verifyRepo = new EventRepository(verifyContext);
        var savedEvent = await verifyRepo.GetByIdAsync(@event.Id);
        Assert.Equal("Updated Description", savedEvent!.Description);
    }
}