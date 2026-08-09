namespace EventMgtApi.Domain.Enums;

/// <summary>
/// Статус бронирования места на событии.
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Бронь создана, ожидает обработки.
    /// </summary>
    Pending,

    /// <summary>
    /// Бронь подтверждена.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Бронь отклонена.
    /// </summary>
    Rejected,

    /// <summary>
    /// Бронь отменена.
    /// </summary>
    Cancelled
}
