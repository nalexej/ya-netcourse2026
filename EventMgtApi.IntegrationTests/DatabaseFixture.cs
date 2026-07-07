using Testcontainers.PostgreSql;

namespace EventMgtApi.IntegrationTests;

/// <summary>
/// Определяет коллекцию тестов, использующих общий экземпляр PostgreSQL-контейнера.
/// xUnit не поддерживает IAsyncDisposable в фикстурах — поэтому используем финализатор для остановки контейнера.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    // Этот класс должен быть пустым — он служит меткой для xUnit
}

/// <summary>
/// Общая фикстура для всех интеграционных тестов.
/// Запускает один экземпляр PostgreSQL через Testcontainers и останавливает его после завершения всех тестов.
/// </summary>
public class PostgreSqlContainerFixture
{
    public PostgreSqlContainer PostgreSql { get; }

    public PostgreSqlContainerFixture()
    {
        PostgreSql = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("eventapi")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        // Запускаем контейнер синхронно ( допустимо в конструкторе)
        PostgreSql.StartAsync().GetAwaiter().GetResult();
    }

    // Финализатор — гарантирует остановку контейнера после завершения всех тестов
    ~PostgreSqlContainerFixture()
    {
        try
        {
            PostgreSql.StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Игнорируем ошибки при остановке (например, если контейнер уже остановлен)
        }
    }
}