namespace EventMgtApi.EventsService.Application.Caching;

public static class EventCacheKeys
{
    public const string EventPrefix = "event:";
    public const string TopEvents = "events:top10";
    public static string ForEvent(Guid eventId) => $"{EventPrefix}{eventId}";
}