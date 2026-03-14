using System.Text.Json;
using MediaPlatform.Application.Abstractions;
using StackExchange.Redis;

namespace MediaPlatform.Infrastructure.Redis;

/// <summary>
/// Redis-backed player log store. Each player gets a capped list (ring buffer)
/// storing the most recent 1000 log entries in JSON.
/// </summary>
public sealed class RedisPlayerLogStore(IConnectionMultiplexer redis) : IPlayerLogStore
{
    private const string KeyPrefix = "player:logs:";
    private const int MaxEntries = 1000;

    private IDatabase Db => redis.GetDatabase();

    public async Task AppendLogsAsync(string playerId, IReadOnlyList<PlayerLogEntry> entries, CancellationToken ct = default)
    {
        if (entries.Count == 0) return;
        var key = $"{KeyPrefix}{playerId}";

        var values = entries.Select(e => (RedisValue)JsonSerializer.Serialize(e)).ToArray();
        await Db.ListRightPushAsync(key, values);
        await Db.ListTrimAsync(key, -MaxEntries, -1);
    }

    public async Task<PlayerLogPage> GetLogsAsync(string playerId, string? level = null, int limit = 100, CancellationToken ct = default)
    {
        var key = $"{KeyPrefix}{playerId}";
        var all = await Db.ListRangeAsync(key);

        var entries = all
            .Select(v => JsonSerializer.Deserialize<PlayerLogEntry>(v.ToString()))
            .Where(e => e is not null)
            .Cast<PlayerLogEntry>()
            .ToList();

        var totalCount = entries.Count;

        if (!string.IsNullOrEmpty(level))
        {
            entries = entries.Where(e => e.Level.Equals(level, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var page = entries.TakeLast(limit).ToList();

        return new PlayerLogPage(playerId, page, totalCount);
    }
}
