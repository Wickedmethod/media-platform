namespace MediaPlatform.Application.Abstractions;

/// <summary>
/// Stores and retrieves player network connectivity metrics.
/// </summary>
public interface INetworkMetricsStore
{
    Task SaveMetricsAsync(string playerId, NetworkMetrics metrics, CancellationToken ct = default);
    Task<NetworkMetrics?> GetCurrentAsync(string playerId, CancellationToken ct = default);
    Task<NetworkTrend> GetTrendAsync(string playerId, CancellationToken ct = default);
}

public record NetworkMetrics(
    string PlayerId,
    string Timestamp,
    LatencyMetrics Latency,
    DnsMetrics Dns,
    BandwidthMetrics Bandwidth);

public record LatencyMetrics(
    int AvgMs,
    int MinMs,
    int MaxMs,
    int P95Ms,
    int Samples,
    int Failures);

public record DnsMetrics(
    int AvgResolveMs,
    int Failures);

public record BandwidthMetrics(
    double LastMbps,
    string MeasuredAt);

public record NetworkTrend(
    string LatencyTrend,   // stable | degrading | improving
    int AvgLatency1h,
    string BandwidthTrend, // stable | degrading | improving
    double AvgBandwidth1h);
