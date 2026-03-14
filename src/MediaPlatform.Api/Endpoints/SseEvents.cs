namespace MediaPlatform.Api.Endpoints;

/// <summary>SSE event payload contracts for the /events stream.</summary>
public static class SseEvents
{
    public sealed record StateChanged(string State);
    public sealed record TrackChanged(QueueItemResponse Item, double Position);
    public sealed record PositionUpdated(double Position, double Duration);
    public sealed record QueueUpdated(string Action, int? Count = null);
    public sealed record ItemAdded(string Id, string Title, string Url, string? AddedByUserId = null, string? AddedByName = null);
    public sealed record KillSwitchToggled(bool Active);
    public sealed record PlaybackError(string Error, string VideoId, int RetryCount);
    public sealed record PolicyChanged(string Action);
    public sealed record Heartbeat;
    public sealed record PlayerOffline(string PlayerId);
    public sealed record PlayerOnline(string PlayerId, string Name);
    public sealed record PlayerDisconnected(string PlayerId, string Reason);
    public sealed record UpdateAvailable(string Version, string Message);
}
