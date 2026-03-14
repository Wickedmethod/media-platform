namespace MediaPlatform.Application.Abstractions;

/// <summary>
/// Tracks player registrations, heartbeats, and liveness status.
/// </summary>
public interface IPlayerRegistry
{
    Task RecordHeartbeatAsync(PlayerHeartbeat heartbeat, CancellationToken ct = default);
    Task<IReadOnlyList<PlayerStatus>> GetAllPlayersAsync(CancellationToken ct = default);
    Task<WorkerRegistrationResult> RegisterAsync(WorkerRegistration registration, CancellationToken ct = default);
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
    string? Version,
    string? Name = null,
    WorkerCapabilities? Capabilities = null,
    DateTimeOffset? RegisteredAt = null);

public record WorkerRegistration(
    string Name,
    WorkerCapabilities? Capabilities,
    string? Version,
    string? Os);

public record WorkerCapabilities(
    bool Cec = false,
    string? AudioOutput = null,
    string? MaxResolution = null,
    IReadOnlyList<string>? Codecs = null,
    string? ChromiumVersion = null);

public record WorkerRegistrationResult(
    string PlayerId,
    DateTimeOffset ServerTime,
    WorkerConfig Config);

public record WorkerConfig(
    int HeartbeatInterval = 30,
    int PositionReportInterval = 5,
    string SseUrl = "/events");
