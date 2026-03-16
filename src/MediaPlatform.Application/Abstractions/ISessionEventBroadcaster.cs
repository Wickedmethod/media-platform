namespace MediaPlatform.Application.Abstractions;

public interface ISessionEventBroadcaster
{
    void Broadcast(string sessionId, string eventType, object data);
    IAsyncEnumerable<(string EventType, string Json)> SubscribeAsync(string sessionId, CancellationToken ct);
}
