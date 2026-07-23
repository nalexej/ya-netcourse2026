using EventMgtApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventMgtApi.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Конфигурация сущности <see cref="Booking"/> для ORM.
    /// Определяет отображение свойств и отношений в таблице "bookings".
    /// </summary>
    internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        /// <summary>
        /// Настраивает отображение сущности <see cref="Booking"/> на таблицу базы данных.
        /// </summary>
        /// <param name="builder">Метод построителя сущности для настройки маппинга.</param>
        public void Configure(EntityTypeBuilder<Booking> builder)
        {

            // Указываем имя таблицы в БД
            builder.ToTable("bookings");

            // Устанавливаем первичный ключ
            builder.HasKey(b => b.Id);

            // Уникальный идентификатор бронирования
            builder.Property(b => b.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            // Внешний ключ к событию (обязательное поле)
            builder.Property(b => b.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            // Статус бронирования: преобразуется в строковое представление enum'а
            builder.Property(b => b.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasConversion<string>() // Сохраняет как строку
                .HasMaxLength(20);

            // Дата и время создания бронирования
            builder.Property(b => b.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            // Дата и время обработки бронирования (может быть null)
            builder.Property(b => b.ProcessedAt)
                .HasColumnName("processed_at");

            // Связь "многие к одному" с событием:
            // каждое бронирование относится к одному событию
            builder.HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
