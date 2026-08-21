namespace EventMgtApi.Contracts.ServiceInteraction;

/// <summary>
/// Константы, связанные с событиями межсервисного взаимодействия.
/// </summary>
public static class ServiceInteractionConstants
{
    /// <summary>
    /// Имя топика (очереди) для события подтверждения брони.
    /// Издатель (BookingsService) публикует, а подписчик (EventsService) подписывается на этот топик.
    /// </summary>
    public const string BookingConfirmedTopic = "booking-confirmed";

    /// <summary>
    /// Имя топика для события отмены брони.
    /// Издатель (BookingsService) публикует, а подписчик (EventsService) подписывается на этот топик.
    /// </summary>
    public const string BookingCancelledTopic = "booking-cancelled";

    /// <summary>
    /// Имя топика для события отмены подтверждения брони (не хватило мест или событие началось).
    /// Издатель (EventsService) публикует, а подписчик (BookingsService) подписывается на этот топик.
    /// </summary>
    public const string BookingConfirmationFailedTopic = "booking-confirmation-failed";
}
