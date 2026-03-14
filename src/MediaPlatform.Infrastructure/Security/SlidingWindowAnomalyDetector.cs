using System.Collections.Concurrent;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Infrastructure.Security;

public sealed class SlidingWindowAnomalyDetector : IAnomalyDetector
{
    private readonly ConcurrentQueue<RequestRecord> _requests = new();
    private readonly TimeSpan _window = TimeSpan.FromMinutes(5);
    private const int DeniedThreshold = 20;
    private const int RequestSpikeThreshold = 200;

    public void RecordRequest(string endpoint, bool denied, string? userId = null)
    {
        _requests.Enqueue(new RequestRecord(endpoint, denied, userId, DateTimeOffset.UtcNow));
        PruneOld();
    }

    public AnomalyReport Evaluate()
    {
        PruneOld();
        var snapshot = _requests.ToArray();
        var alerts = new List<AnomalyAlert>();

        // Rule 1: High volume of denied requests
        var deniedCount = snapshot.Count(r => r.Denied);
        if (deniedCount >= DeniedThreshold)
        {
            alerts.Add(new AnomalyAlert(
                "high-denial-rate",
                $"{deniedCount} denied requests in {_window.TotalMinutes}min window",
                "critical",
                DateTimeOffset.UtcNow));
        }

        // Rule 2: Request spike (total volume)
        if (snapshot.Length >= RequestSpikeThreshold)
        {
            alerts.Add(new AnomalyAlert(
                "request-spike",
                $"{snapshot.Length} requests in {_window.TotalMinutes}min window",
                "warning",
                DateTimeOffset.UtcNow));
        }

        // Rule 3: Single user/IP with many denied requests
        var perUser = snapshot.Where(r => r.Denied && r.UserId is not null)
            .GroupBy(r => r.UserId)
            .Where(g => g.Count() >= 10);

        foreach (var group in perUser)
        {
            alerts.Add(new AnomalyAlert(
                "user-abuse",
                $"User {group.Key} had {group.Count()} denied requests in window",
                "critical",
                DateTimeOffset.UtcNow));
        }

        return new AnomalyReport(alerts.Count > 0, alerts);
    }

    private void PruneOld()
    {
        var cutoff = DateTimeOffset.UtcNow - _window;
        while (_requests.TryPeek(out var oldest) && oldest.Timestamp < cutoff)
            _requests.TryDequeue(out _);
    }

    private record RequestRecord(string Endpoint, bool Denied, string? UserId, DateTimeOffset Timestamp);
}
