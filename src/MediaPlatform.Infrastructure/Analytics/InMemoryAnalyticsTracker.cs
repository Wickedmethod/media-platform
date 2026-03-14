using System.Collections.Concurrent;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Infrastructure.Analytics;

public sealed class InMemoryAnalyticsTracker : IAnalyticsTracker
{
    private readonly ConcurrentBag<CommandRecord> _commands = [];
    private readonly ConcurrentBag<ErrorRecord> _errors = [];
    private double _totalPlaybackSeconds;
    private readonly Lock _lock = new();

    public void RecordCommand(string commandType, double latencyMs)
    {
        _commands.Add(new CommandRecord(commandType, latencyMs, DateTimeOffset.UtcNow));
    }

    public void RecordPlaybackTime(double seconds)
    {
        lock (_lock) { _totalPlaybackSeconds += seconds; }
    }

    public void RecordError(string reason)
    {
        _errors.Add(new ErrorRecord(reason, DateTimeOffset.UtcNow));
    }

    public AnalyticsSnapshot GetSnapshot(DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var start = from ?? DateTimeOffset.MinValue;
        var end = to ?? DateTimeOffset.MaxValue;

        var commands = _commands.Where(c => c.OccurredAt >= start && c.OccurredAt <= end).ToList();
        var errors = _errors.Where(e => e.OccurredAt >= start && e.OccurredAt <= end).ToList();

        var commandCounts = commands
            .GroupBy(c => c.CommandType)
            .ToDictionary(g => g.Key, g => g.Count()) as IReadOnlyDictionary<string, int>;

        var avgLatency = commands.Count > 0 ? commands.Average(c => c.LatencyMs) : 0;

        double playbackSeconds;
        lock (_lock) { playbackSeconds = _totalPlaybackSeconds; }

        return new AnalyticsSnapshot(
            TotalCommands: commands.Count,
            TotalErrors: errors.Count,
            TotalPlaybackSeconds: Math.Round(playbackSeconds, 1),
            AverageCommandLatencyMs: Math.Round(avgLatency, 2),
            CommandCounts: commandCounts,
            RecentErrors: errors.OrderByDescending(e => e.OccurredAt).Take(50).ToList().AsReadOnly(),
            From: start == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow.AddDays(-30) : start,
            To: end == DateTimeOffset.MaxValue ? DateTimeOffset.UtcNow : end);
    }

    private record CommandRecord(string CommandType, double LatencyMs, DateTimeOffset OccurredAt);
}
