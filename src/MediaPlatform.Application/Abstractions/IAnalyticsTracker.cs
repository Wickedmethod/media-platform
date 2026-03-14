namespace MediaPlatform.Application.Abstractions;

public interface IAnalyticsTracker
{
    void RecordCommand(string commandType, double latencyMs);
    void RecordPlaybackTime(double seconds);
    void RecordError(string reason);
    AnalyticsSnapshot GetSnapshot(DateTimeOffset? from = null, DateTimeOffset? to = null);
}

public record AnalyticsSnapshot(
    int TotalCommands,
    int TotalErrors,
    double TotalPlaybackSeconds,
    double AverageCommandLatencyMs,
    IReadOnlyDictionary<string, int> CommandCounts,
    IReadOnlyList<ErrorRecord> RecentErrors,
    DateTimeOffset From,
    DateTimeOffset To);

public record ErrorRecord(string Reason, DateTimeOffset OccurredAt);
