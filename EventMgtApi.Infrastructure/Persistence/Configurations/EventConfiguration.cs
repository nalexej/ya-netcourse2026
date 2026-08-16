using EventMgtApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventMgtApi.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Конфигурация сущности <see cref="Event"/> для ORM.
    /// Определяет отображение свойств и отношений в таблице "events".
    /// </summary>
    internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        /// <summary>
        /// Настраивает отображение сущности <see cref="Event"/> на таблицу базы данных.
        /// </summary>
        /// <param name="builder">Метод построителя сущности для настройки маппинга.</param>
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            // Указываем имя таблицы в БД
            builder.ToTable("events");

            // Устанавливаем первичный ключ
            builder.HasKey(e => e.Id);

            // Идентификатор события
            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            // Заголовок события (обязательное поле)
            builder.Property(e => e.Title)
                .HasColumnName("title")
                .IsRequired()
                .HasMaxLength(200);

            // Описание события (опционально)
            builder.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(2000);

            // Дата и время начала события
            builder.Property(e => e.StartAt)
                .HasColumnName("start_at")
                .IsRequired();

            // Дата и время окончания события
            builder.Property(e => e.EndAt)
                .HasColumnName("end_at")
                .IsRequired();

            // Общее количество мест (обязательное)
            builder.Property(e => e.TotalSeats)
                .HasColumnName("total_seats")
                .IsRequired();

            // Количество доступных мест (обязательное)
            builder.Property(e => e.AvailableSeats)
                .HasColumnName("available_seats")
                .IsRequired();

            // Маркер версии строки для оптимистичного контроля параллелизма
            builder.Property(e => e.RowVersion)
                .HasColumnName("row_version")
                .IsRowVersion();

            // Связь "один ко многим" с бронированиями:
            // одно событие → множество бронирований
            builder.HasMany(e => e.Bookings)
                .WithOne(b => b.Event)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_bookings_events_event_id");
        }
    }
}
