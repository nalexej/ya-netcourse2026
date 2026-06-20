using EventMgtApi.Application.DTOs;
using System.ComponentModel.DataAnnotations;
using ValidationException = EventMgtApi.Domain.Exceptions.ValidationException;

namespace EventMgtApi.Domain.Entities
{
    /// <summary>
    /// Представляет модель события в системе управления событиями.
    /// Содержит основные данные: идентификатор, заголовок, описание и временной диапазон.
    /// </summary>
    public class Event
    {

        #region Properties

        /// <summary>
        /// Уникальный идентификатор события.
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Заголовок (название) события.
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Описание события. Необязательное поле.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Дата и время начала события.
        /// </summary>
        public required DateTime StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания события.
        /// </summary>
        public required DateTime EndAt { get; set; }

        /// <summary>
        /// Общее количество мест на событии (обязательное, > 0).
        /// </summary>
        public required int TotalSeats { get; set; }

        /// <summary>
        /// Текущее количество свободных мест. При создании события равно TotalSeats.
        /// </summary>
        public int AvailableSeats { get; set; }

        /// <summary>
        /// Навигационное свойство: список бронирований.
        /// </summary>
        public ICollection<Booking> Bookings { get; private set; } = [];
 
        #endregion

        #region Constructors

        // Приватный конструктор без параметров для ORM
        private Event() { }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новое событие с валидацией TotalSeats.
        /// </summary>
        /// <param name="title">Заголовок события.</param>
        /// <param name="startAt">Дата и время начала.</param>
        /// <param name="endAt">Дата и время окончания.</param>
        /// <param name="totalSeats">Общее количество мест (должно быть больше 0).</param>
        /// <param name="description">Описание события (опционально).</param>
        /// <returns>Созданная сущность Event.</returns>
        /// <exception cref="ValidationException">Если totalSeats меньше либо равно 0.</exception>
        public static Event Create(string? title, DateTime startAt, DateTime endAt, int totalSeats, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ValidationException("Заголовок обязателен.");

            if (totalSeats <= 0)
                throw new ValidationException("Общее количество мест должно быть больше нуля.");

            if (startAt >= endAt)
                throw new ValidationException("Дата начала должна быть раньше даты окончания.");

            return new Event
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description ?? string.Empty,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Пытается зарезервировать указанное количество мест.
        /// Возвращает false, если свободных мест недостаточно;
        /// уменьшает AvailableSeats на count и возвращает true, если места есть.
        /// </summary>
        /// <param name="count">Количество мест для резервирования (по умолчанию 1).</param>
        /// <returns>true, если резервирование успешно; false, если недостаточно мест.</returns>
        public bool TryReserveSeats(int count = 1)
        {
            if (AvailableSeats < count)
            {
                return false;
            }

            AvailableSeats -= count;
            return true;
        }

        /// <summary>
        /// Освобождает указанное количество мест.
        /// Увеличивает AvailableSeats на count.
        /// </summary>
        /// <param name="count">Количество мест для освобождения (по умолчанию 1).</param>
        public void ReleaseSeats(int count = 1)
        {
            if (count <= 0) return;

            AvailableSeats = Math.Min(AvailableSeats + count, TotalSeats);
        }

        #endregion

    }
}
