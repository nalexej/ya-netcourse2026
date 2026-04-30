using System.ComponentModel.DataAnnotations;

namespace EventMgtApi.Models
{
    /// <summary>
    /// Представляет модель события в системе управления событиями.
    /// Содержит основные данные: идентификатор, заголовок, описание и временной диапазон.
    /// </summary>
    public class Event
    {
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
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Дата и время начала события.
        /// </summary>
        public required DateTime StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания события.
        /// </summary>
        public required DateTime EndAt { get; set; }
    }
}