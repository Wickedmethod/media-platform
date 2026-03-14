namespace MediaPlatform.Api.Endpoints;

public sealed record AddToQueueRequest(string Url, string Title, double StartAtSeconds = 0);

public sealed record QueueItemResponse(
    string Id, string Url, string Title, string Status, DateTimeOffset AddedAt, double StartAtSeconds,
    string? AddedByUserId = null, string? AddedByName = null);

public sealed record PlaybackStateResponse(
    string State, QueueItemResponse? CurrentItem, DateTimeOffset? StartedAt,
    double PositionSeconds, int RetryCount, string? LastError);

public sealed record ReportPositionRequest(double PositionSeconds);
public sealed record ReportErrorRequest(string Reason);
public sealed record SetQueueModeRequest(string Mode);

public sealed record QueueModeResponse(string Mode);

public sealed record ApiError(string Error, string? Detail = null);

// ── MEDIA-725: Queue Snapshot ──────────────────────────────

public sealed record SyncSnapshot(
    IEnumerable<QueueItemResponse> Queue,
    PlaybackStateResponse NowPlaying,
    string QueueMode,
    IReadOnlyList<PolicySnapshot> Policies,
    bool KillSwitch,
    DateTimeOffset ServerTime,
    long Version);

public sealed record PolicySnapshot(string Id, string Name, string Type, bool Enabled);

// ── MEDIA-724: Player Heartbeat ────────────────────────────

public sealed record HeartbeatRequest(
    string PlayerId,
    string State,
    double Position = 0,
    string? VideoId = null,
    long Uptime = 0,
    string? Version = null);

public sealed record PlayerStatusResponse(
    string Id,
    DateTimeOffset LastSeen,
    string State,
    bool IsAlive,
    long Uptime,
    string? Version,
    string? Name = null,
    object? Capabilities = null,
    DateTimeOffset? RegisteredAt = null);

// ── MEDIA-729: Worker Registration ─────────────────────────

public sealed record WorkerRegistrationRequest(
    string Name,
    WorkerCapabilitiesDto? Capabilities = null,
    string? Version = null,
    string? Os = null);

public sealed record WorkerCapabilitiesDto(
    bool Cec = false,
    string? AudioOutput = null,
    string? MaxResolution = null,
    IReadOnlyList<string>? Codecs = null,
    string? ChromiumVersion = null);

public sealed record WorkerRegistrationResponse(
    string PlayerId,
    DateTimeOffset ServerTime,
    WorkerConfigResponse Config);

public sealed record WorkerConfigResponse(
    int HeartbeatInterval,
    int PositionReportInterval,
    string SseUrl);
