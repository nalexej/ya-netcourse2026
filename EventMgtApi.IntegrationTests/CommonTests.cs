using EventMgtApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventMgtApi.IntegrationTests;

/// <summary>
/// Тест: проверка корректного применения миграций и структуры БД.
/// Проверяет:
/// - таблицы Events и Bookings существуют,
/// - внешний ключ FK_bookings_events_event_id создан,
/// - миграции применяются без ошибок.
/// </summary>
[Collection("Database")]
public class CommonTests
{
    private readonly PostgreSqlContainer _postgres;

    public CommonTests(PostgreSqlContainerFixture fixture)
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
    public async Task MigrateAsync_ShouldCreateTablesAndForeignKeys()
    {
        // Arrange: пустая БД (тестcontainers создаёт свежий инстанс)
        await ResetDatabaseAsync();

        // Создаём СВЕЖИЙ контекст, но соединение будет закрыто → открываем явно
        var context = CreateContext();

        // Assert: проверяем структуру
        await AssertTablesExist(context);
        await AssertForeignKeyExists(context);
    }

    private async Task AssertTablesExist(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        var tableNames = await context.Database
            .SqlQueryRaw<string>("SELECT LOWER(tablename) FROM pg_tables WHERE schemaname = 'public'")
            .ToListAsync();

        Assert.Contains("events", tableNames);
        Assert.Contains("bookings", tableNames);
    }
    private async Task AssertForeignKeyExists(AppDbContext context)
    {
        var fkNames = await context.Database
            .SqlQueryRaw<string>("SELECT conname FROM pg_constraint WHERE conname = 'FK_bookings_events_event_id'")
            .ToListAsync();

        Assert.Contains("FK_bookings_events_event_id", fkNames);
    }
}