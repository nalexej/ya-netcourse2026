using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventMgtApi.Exceptions
{
    /// <summary>
    /// Исключение, выбрасываемое при ошибке валидации входных данных.
    /// Например: неверный формат, отсутствующие обязательные поля.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Получает словарь состояния модели, содержащий ошибки валидации.
        /// Доступен, если исключение было вызвано проверкой <see cref="ModelStateDictionary"/>.
        /// </summary>
        public ModelStateDictionary ModelState { get; } = null!;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidationException"/> с заданным сообщением об ошибке.
        /// </summary>
        /// <param name="message">Сообщение, описывающее причину исключения.</param>
        public ValidationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidationException"/> 
        /// с заданным сообщением и внутренним исключением, которое стало причиной этого исключения.
        /// </summary>
        /// <param name="message">Сообщение об ошибке, объясняющее причину исключения.</param>
        /// <param name="innerException">Внутреннее исключение, вызвавшее данное исключение.</param>
        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidationException"/> 
        /// с сообщением по умолчанию: "Обнаружены ошибки валидации входных данных."
        /// </summary>
        public ValidationException()
            : base("Обнаружены ошибки валидации входных данных.")
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidationException"/> 
        /// на основе <see cref="ModelStateDictionary"/> с деталями валидации.
        /// </summary>
        /// <param name="modelState">
        /// Словарь состояния модели, содержащий ошибки валидации. 
        /// Не должен быть <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="modelState"/> равен <see langword="null"/>.</exception>
        public ValidationException(ModelStateDictionary modelState)
            : base("Обнаружены ошибки валидации входных данных.")
        {
            ModelState = modelState ?? throw new ArgumentNullException(nameof(modelState));
        }
    }
}