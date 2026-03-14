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

// ── MEDIA-732: Player Log Streaming ────────────────────────

public sealed record SubmitLogsRequest(
    string PlayerId,
    IReadOnlyList<LogEntryRequest> Entries);

public sealed record LogEntryRequest(
    string Timestamp,
    string Level,
    string Message,
    string? Source = null);

public sealed record PlayerLogResponse(
    string PlayerId,
    IReadOnlyList<LogEntryResponse> Entries,
    int TotalCount);

public sealed record LogEntryResponse(
    string Timestamp,
    string Level,
    string Message,
    string? Source);

// ── MEDIA-733: Player Version & Update Check ───────────────

public sealed record VersionMatrixResponse(
    string? ExpectedVersion,
    IReadOnlyList<PlayerVersionInfo> Players);

public sealed record PlayerVersionInfo(
    string Id,
    string? Version,
    bool UpToDate);

public sealed record SetExpectedVersionRequest(string Version);

public sealed record NotifyUpdateRequest(string Message);

// ── MEDIA-760: Graceful Disconnect ─────────────────────────

public sealed record DisconnectRequest(string Reason, string? Signal = null);

// ── MEDIA-763: Network Connectivity Monitoring ─────────────

public sealed record SubmitNetworkMetricsRequest(
    string PlayerId,
    string Timestamp,
    LatencyMetricsDto Latency,
    DnsMetricsDto Dns,
    BandwidthMetricsDto Bandwidth);

public sealed record LatencyMetricsDto(
    int AvgMs, int MinMs, int MaxMs, int P95Ms, int Samples, int Failures);

public sealed record DnsMetricsDto(int AvgResolveMs, int Failures);

public sealed record BandwidthMetricsDto(double LastMbps, string MeasuredAt);

public sealed record NetworkMetricsResponse(
    NetworkMetricsCurrentDto? Current,
    NetworkTrendDto Trend);

public sealed record NetworkMetricsCurrentDto(
    string PlayerId, string Timestamp,
    LatencyMetricsDto Latency, DnsMetricsDto Dns, BandwidthMetricsDto Bandwidth);

public sealed record NetworkTrendDto(
    string LatencyTrend, int AvgLatency1h,
    string BandwidthTrend, double AvgBandwidth1h);

// ── MEDIA-743: Alerting ────────────────────────────────────

public sealed record AlertConfigResponse(
    bool Enabled, int CooldownMinutes, int ChannelCount);
