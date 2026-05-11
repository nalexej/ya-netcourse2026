using System.ComponentModel.DataAnnotations;

namespace EventMgtApi.Models.Dto;

    /// <summary>
    /// Представляет модель события в системе управления событиями.
    /// Содержит основные данные: заголовок, описание и временной диапазон.
    /// </summary>
    [CustomValidation(typeof(EventDto), nameof(ValidateDateRange))]
    public class EventDto
    {
        /// <summary>
        /// Заголовок (название) события. Обязательное поле.
        /// </summary>
        [Required(ErrorMessage = "Заголовок обязателен.")]
        public required string Title { get; set; }

        /// <summary>
        /// Описание события. Необязательное поле.
        /// По умолчанию — пустая строка, чтобы избежать null-значений.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Дата и время начала события. Обязательное поле.
        /// Должно быть указано при создании события.
        /// </summary>
        [Required(ErrorMessage = "Дата начала обязательна.")]
        public required DateTime StartAt { get; set; }

        /// <summary>
        /// Дата и время окончания события. Обязательное поле.
        /// Помимо проверки на наличие, проходит дополнительную логическую проверку:
        /// должна быть больше, чем <see cref="StartAt"/>.
        /// Для этого используется кастомная валидация через <see cref="CustomValidationAttribute"/>.
        /// </summary>
        [Required(ErrorMessage = "Дата окончания обязательна.")]
        public required DateTime EndAt { get; set; }

        /// <summary>
        /// Статический метод, используемый для кастомной валидации диапазона дат.
        /// Проверяет, что дата начала события (<see cref="StartAt"/>) строго меньше даты окончания (<see cref="EndAt"/>).
        /// </summary>
        /// <param name="instance">Экземпляр класса <see cref="EventDto"/>, который проходит валидацию.</param>
        /// <param name="validationContext">Контекст, содержащий информацию о процессе валидации.</param>
        /// <returns>
        /// Возвращает <see cref="ValidationResult.Success"/> если условие выполнено;
        /// иначе — объект <see cref="ValidationResult"/> с сообщением об ошибке и указанием затронутых свойств.
        /// </returns>
        public static ValidationResult ValidateDateRange(object instance, ValidationContext validationContext)
        {
            // Защита от null
            if (instance == null)
                return new ValidationResult("Данные события не могут быть null.");
    
            var eventObj = (EventDto)instance;
            if (eventObj.StartAt >= eventObj.EndAt)
            {
                return new ValidationResult(
                    "Дата начала должна быть раньше даты окончания.",
                    new[] { nameof(eventObj.StartAt), nameof(eventObj.EndAt) } // Указывает, какие поля нарушили правило
                );
            }
            return ValidationResult.Success!;
        }
    }
