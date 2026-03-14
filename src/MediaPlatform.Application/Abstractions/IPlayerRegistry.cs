namespace MediaPlatform.Application.Abstractions;

/// <summary>
/// Tracks player heartbeats and liveness status.
/// </summary>
public interface IPlayerRegistry
{
    Task RecordHeartbeatAsync(PlayerHeartbeat heartbeat, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerStatus>> GetAllPlayersAsync(CancellationToken ct = default);
}

public record PlayerHeartbeat(
    string PlayerId,
    string State,
    double Position,
    string? VideoId,
    long Uptime,
    string? Version);

public record PlayerStatus(
    string Id,
    DateTimeOffset LastSeen,
    string State,
    bool IsAlive,
    long Uptime,
    string? Version);
