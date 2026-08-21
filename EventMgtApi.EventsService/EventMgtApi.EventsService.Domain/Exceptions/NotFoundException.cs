using System;

namespace EventMgtApi.EventsService.Domain.Exceptions
{
    /// <summary>
    /// Исключение, выбрасываемое, когда запрашиваемый ресурс не найден.
    /// </summary>
    public class NotFoundException : Exception
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NotFoundException"/> с заданным сообщением об ошибке.
        /// </summary>
        /// <param name="message">Сообщение, описывающее причину исключения.</param>
        public NotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NotFoundException"/> 
        /// с заданным сообщением и внутренним исключением, которое стало причиной этого исключения.
        /// </summary>
        /// <param name="message">Сообщение об ошибке, объясняющее причину исключения.</param>
        /// <param name="innerException">Внутреннее исключение, вызвавшее данное исключение.</param>
        public NotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NotFoundException"/> 
        /// с сообщением по умолчанию: "Запрашиваемый ресурс не найден."
        /// </summary>
        public NotFoundException()
            : base("Запрашиваемый ресурс не найден.")
        {
        }
    }
}