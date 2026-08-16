using EventMgtApi.Domain.Entities;
using EventMgtApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventMgtApi.Infrastructure.Persistence.Configurations;

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

        // Связь "один ко многим" с бронированиями:
        // один пользователь → множество бронирований
        builder.HasMany(u => u.Bookings)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_bookings_users_user_id");
    }
}
