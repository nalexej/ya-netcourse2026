namespace EventMgtApi.Contracts.ServiceInteraction
{
    public interface IEventPublisher : IDisposable
    {
        Task PublishAsync<T>(T eventMessage, string? key = null, CancellationToken ct = default) where T : class;
    }
}
