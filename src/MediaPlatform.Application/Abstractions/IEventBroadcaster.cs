namespace MediaPlatform.Application.Abstractions;

public interface IEventBroadcaster
{
    void Broadcast(string eventType, object data);
    IAsyncEnumerable<(string EventType, string Json)> SubscribeAsync(CancellationToken ct);
}
