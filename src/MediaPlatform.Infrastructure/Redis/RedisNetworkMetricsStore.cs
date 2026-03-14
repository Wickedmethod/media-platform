using System.Text.Json;
using MediaPlatform.Application.Abstractions;
using StackExchange.Redis;

namespace MediaPlatform.Infrastructure.Redis;

public sealed class RedisNetworkMetricsStore(IConnectionMultiplexer redis) : INetworkMetricsStore
{
    private static readonly TimeSpan HistoryTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static string CurrentKey(string playerId) => $"network:metrics:{playerId}";
    private static string HistoryKey(string playerId) => $"network:metrics:{playerId}:history";

    public async Task SaveMetricsAsync(string playerId, NetworkMetrics metrics, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(metrics, JsonOpts);
        var score = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Store latest
        await db.StringSetAsync(CurrentKey(playerId), json, HistoryTtl);

        // Append to history sorted set
        await db.SortedSetAddAsync(HistoryKey(playerId), json, score);

        // Trim entries older than 24h
        var cutoff = DateTimeOffset.UtcNow.Add(-HistoryTtl).ToUnixTimeSeconds();
        await db.SortedSetRemoveRangeByScoreAsync(HistoryKey(playerId), double.NegativeInfinity, cutoff);
        await db.KeyExpireAsync(HistoryKey(playerId), HistoryTtl);
    }

    public async Task<NetworkMetrics?> GetCurrentAsync(string playerId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var json = await db.StringGetAsync(CurrentKey(playerId));
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<NetworkMetrics>((string)json!, JsonOpts);
    }

    public async Task<NetworkTrend> GetTrendAsync(string playerId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var oneHourAgo = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var entries = await db.SortedSetRangeByScoreAsync(HistoryKey(playerId), oneHourAgo, now);

        if (entries.Length == 0)
            return new NetworkTrend("stable", 0, "stable", 0);

        var metrics = entries
            .Select(e => JsonSerializer.Deserialize<NetworkMetrics>((string)e!, JsonOpts))
            .Where(m => m is not null)
            .Cast<NetworkMetrics>()
            .ToList();

        if (metrics.Count == 0)
            return new NetworkTrend("stable", 0, "stable", 0);

        var avgLatency = (int)metrics.Average(m => m.Latency.AvgMs);
        var avgBandwidth = metrics.Average(m => m.Bandwidth.LastMbps);

        // Determine trends by comparing first half vs second half
        var half = metrics.Count / 2;
        var latencyTrend = "stable";
        var bandwidthTrend = "stable";

        if (half > 0 && metrics.Count > 1)
        {
            var firstHalfLatency = metrics.Take(half).Average(m => m.Latency.AvgMs);
            var secondHalfLatency = metrics.Skip(half).Average(m => m.Latency.AvgMs);
            if (secondHalfLatency > firstHalfLatency * 1.3) latencyTrend = "degrading";
            else if (secondHalfLatency < firstHalfLatency * 0.7) latencyTrend = "improving";

            var firstHalfBw = metrics.Take(half).Average(m => m.Bandwidth.LastMbps);
            var secondHalfBw = metrics.Skip(half).Average(m => m.Bandwidth.LastMbps);
            if (secondHalfBw < firstHalfBw * 0.7) bandwidthTrend = "degrading";
            else if (secondHalfBw > firstHalfBw * 1.3) bandwidthTrend = "improving";
        }

        return new NetworkTrend(latencyTrend, avgLatency, bandwidthTrend, avgBandwidth);
    }
}
