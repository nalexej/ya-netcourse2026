using System.ComponentModel.DataAnnotations;

namespace EventMgtApi.Application.DTOs;

    /// <summary>
    /// Представляет модель события в системе управления событиями.
    /// Содержит основные данные: заголовок, описание и временной диапазон.
    /// </summary>
    public class EventDto : IValidatableObject
    {
        /// <summary>
        /// Заголовок (название) события. Обязательное поле.
        /// </summary>
        [Required(ErrorMessage = "Заголовок обязателен.")]
        public string? Title { get; set; }

        /// <summary>
        /// Описание события. Может быть null при получении.
        /// На уровне сервиса преобразуется в пустую строку, если не задано.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Дата и время начала события. Обязательное поле.
        /// Должно быть указано при создании события.
        /// </summary>
        [Required(ErrorMessage = "Дата начала обязательна.")]
        public DateTime? StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания события. Обязательное поле.
        /// Помимо проверки на наличие, проходит дополнительную логическую проверку:
        /// должна быть больше, чем <see cref="StartAt"/>.
        /// </summary>
        [Required(ErrorMessage = "Дата окончания обязательна.")]
        public DateTime? EndAt { get; set; }


        /// <summary>
        /// Проверяет бизнес-правила для события.
        /// </summary>
        /// <param name="validationContext">Контекст валидации. Передаётся автоматически фреймворком.</param>
        /// <returns>
        /// Коллекция результатов валидации. 
        /// Если ошибок нет — возвращается пустая коллекция.
        /// </returns>        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartAt.HasValue && EndAt.HasValue && StartAt.Value >= EndAt.Value)
            {
                yield return new ValidationResult(
                    "Дата начала должна быть раньше даты окончания.",
                    new[] { nameof(StartAt), nameof(EndAt) });
            }
        }
    }
