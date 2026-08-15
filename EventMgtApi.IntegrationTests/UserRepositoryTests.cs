using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using EventMgtApi.Infrastructure.Persistence;
using EventMgtApi.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using EventMgtApi.Infrastructure.Repositories;

namespace EventMgtApi.IntegrationTests
{
    [Collection("Database")]
    public class UserRepositoryTests
    {
        private readonly PostgreSqlContainer _postgres;

        public UserRepositoryTests(PostgreSqlContainerFixture fixture)
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
        public async Task CreateUserAsync_DuplicateLogin_ShouldThrowDbUpdateException()
        {
            // Arrange
            await ResetDatabaseAsync();
            var context = CreateContext();

            var login = "testuser_unique_login";
            var passwordHash = "hash123";

            var user1 = User.Create(login, passwordHash, UserRole.User);
            var user2 = User.Create(login, passwordHash, UserRole.Admin);

            context.Users.Add(user1);
            await context.SaveChangesAsync();

            // Act & Assert
            context.Users.Add(user2);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                context.SaveChangesAsync());

            exception.InnerException?.Message.Should().Contain("IX_users_login",
                "Попытка создать пользователя с дублирующимся логином должна вызвать уникальность ограничения.");
        }

        [Fact]
        public async Task CreateUserAsync_ValidLogin_ShouldSucceed()
        {
            // Arrange
            await ResetDatabaseAsync();
            var context = CreateContext();

            var login1 = "testuser_one";
            var login2 = "testuser_two";

            var user1 = User.Create(login1, "hash1", UserRole.User);
            var user2 = User.Create(login2, "hash2", UserRole.User);

            context.Users.Add(user1);
            context.Users.Add(user2);
            await context.SaveChangesAsync();

            // Act — проверяем через новый контекст
            await using var verifyCtx = CreateContext();
            var verifyRepo = new UserRepository(verifyCtx);

            // Assert
            var found1 = await verifyRepo.GetByLoginAsync(login1);
            var found2 = await verifyRepo.GetByLoginAsync(login2);

            found1.Should().NotBeNull();
            found2.Should().NotBeNull();
            found1!.Id.Should().Be(user1.Id);
            found2.Id.Should().Be(user2.Id);
            found1.Login.Should().Be(login1);
            found2.Login.Should().Be(login2);
            found1.Id.Should().NotBe(found2.Id);
        }
    }
}