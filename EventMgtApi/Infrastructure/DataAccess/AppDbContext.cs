using EventMgtApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventMgtApi.Infrastructure.DataAccess
{
    /// <summary>
    /// Контекст базы данных для системы управления событиями.
    /// Предоставляет доступ к наборам сущностей <see cref="Event"/> и <see cref="Booking"/>.
    /// </summary>
    public sealed class AppDbContext : DbContext
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AppDbContext"/> с указанными параметрами.
        /// </summary>
        /// <param name="options">Параметры контекста базы данных, передаваемые из DI-контейнера.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        /// <summary>
        /// Набор сущностей событий (<see cref="Event"/>).
        /// </summary>
        public DbSet<Event> Events => Set<Event>();

        /// <summary>
        /// Набор сущностей бронирований (<see cref="Booking"/>).
        /// </summary>
        public DbSet<Booking> Bookings => Set<Booking>();

        /// <summary>
        /// Выполняется при настройке модели моделирования (Model Builder).
        /// Загружает все конфигурации сущностей из текущей сборки.
        /// </summary>
        /// <param name="modelBuilder">Конструктор модели, используемый для определения схемы базы данных.</param> 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Автоматическое применение всех конфигураций из этой сборки
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
