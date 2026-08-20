using EventMgtApi.Contracts.Enums;
using EventMgtApi.UsersService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventMgtApi.UsersService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Конфигурация сущности <see cref="User"/> для ORM.
/// Определяет отображение свойств и отношений в таблице "users".
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(u => u.Login)
            .HasColumnName("login")
            .IsRequired()
            .HasMaxLength(100);

        // Индекс по логину для быстрого поиска при входе
        builder.HasIndex(u => u.Login)
            .IsUnique()
            .HasDatabaseName("IX_users_login");

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion(
                v => v.ToString(),
                v => (UserRole)Enum.Parse(typeof(UserRole), v))
            .IsRequired();
    }
}
