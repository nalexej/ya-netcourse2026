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
            ThrowIfNotValid(title, startAt, endAt, totalSeats);
            return new Event
            {
                Id = Guid.NewGuid(),
                Title = title!.Trim(),
                Description = description ?? string.Empty,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats
            };
        }

        #endregion

        /// <summary>
        /// Обновляеи существующее событие.
        /// </summary>
        /// <param name="title">Заголовок события.</param>
        /// <param name="startAt">Дата и время начала.</param>
        /// <param name="endAt">Дата и время окончания.</param>
        /// <param name="description">Описание события (опционально).</param>
        /// <returns>Обновленная сущность Event.</returns>
        /// <exception cref="ValidationException">Если totalSeats меньше либо равно 0.</exception>
        public void Update(string? title, DateTime? startAt, DateTime? endAt, string? description = null)
        {
            ThrowIfNotValid(title, startAt, endAt, TotalSeats);
            Title = title!;
            StartAt = startAt!.Value;
            EndAt = endAt!.Value;
            Description = description;
        }

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

        private static void ThrowIfNotValid(
            string? title,
            DateTime? startAt,
            DateTime? endAt,
            int? totalSeats)
        {
            var errors = new Dictionary<string, ICollection<string>>();

            if (string.IsNullOrWhiteSpace(title))
                AddError(errors, nameof(Title), "Заголовок обязателен.");

            if (!startAt.HasValue)
                AddError(errors, nameof(StartAt), "Дата начала обязательна.");

            if (!endAt.HasValue)
                AddError(errors, nameof(EndAt), "Дата окончания обязательна.");

            if (startAt < DateTime.UtcNow)
                AddError(errors, nameof(StartAt), "Дата начала не должна быть в прошлом.");

            if (startAt >= endAt)
                AddError(errors, nameof(StartAt), "Дата начала должна быть раньше даты окончания.");

            if (!totalSeats.HasValue || totalSeats.Value <= 0)
                AddError(errors, nameof(TotalSeats), "Общее количество мест должно быть больше нуля.");

            if (errors.Any())
                throw new ValidationException(errors);

        }

        private static void AddError(Dictionary<string, ICollection<string>> errors, string field, string message)
        {
            if (!errors.ContainsKey(field))
                errors[field] = new List<string>();

            errors[field].Add(message);
        }

        #endregion

    }
}
