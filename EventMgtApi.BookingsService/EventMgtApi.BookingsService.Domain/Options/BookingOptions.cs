namespace EventMgtApi.BookingsService.Domain.Options
{
    /// <summary>
    /// Параметры конфигурации для бронирований.
    /// </summary>
    public class BookingOptions
    {
        /// <summary>
        /// Максимальное количество активных броней для одного пользователя.
        /// </summary>
        public int MaxActiveBookings { get; set; } = 10;
    }
}