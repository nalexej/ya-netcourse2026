namespace EventMgtApi.EventsService.Application.Caching;

public sealed class EventCacheOptions
{
    public const string SectionName = "EventCache";
    public int EventTtlSeconds { get; set; } = 300;
    public int TopEventsTtlSeconds { get; set; } = 300;
}