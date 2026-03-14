using MediaPlatform.Application.Abstractions;
using StackExchange.Redis;

namespace MediaPlatform.Infrastructure.Redis;

public sealed class RedisPlayerRegistry(IConnectionMultiplexer redis) : IPlayerRegistry
{
    private const string KeyPrefix = "player:";
    private const string KeySuffix = ":heartbeat";
    private static readonly TimeSpan HeartbeatTtl = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan ZombieThreshold = TimeSpan.FromSeconds(90);

    private IDatabase Db => redis.GetDatabase();
    private IServer Server => redis.GetServers()[0];

    public async Task RecordHeartbeatAsync(PlayerHeartbeat heartbeat, CancellationToken ct = default)
    {
        var key = $"{KeyPrefix}{heartbeat.PlayerId}{KeySuffix}";
        var entries = new HashEntry[]
        {
            new("lastSeen", DateTimeOffset.UtcNow.ToString("O")),
            new("state", heartbeat.State),
            new("position", heartbeat.Position.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("videoId", heartbeat.VideoId ?? ""),
            new("uptime", heartbeat.Uptime.ToString()),
            new("version", heartbeat.Version ?? "")
        };

        await Db.HashSetAsync(key, entries);
        await Db.KeyExpireAsync(key, HeartbeatTtl);
    }

    public async Task<IReadOnlyList<PlayerStatus>> GetAllPlayersAsync(CancellationToken ct = default)
    {
        var keys = Server.Keys(pattern: $"{KeyPrefix}*{KeySuffix}");
        var players = new List<PlayerStatus>();

        foreach (var key in keys)
        {
            var hash = await Db.HashGetAllAsync(key);
            if (hash.Length == 0) continue;

            var dict = hash.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

            var playerId = key.ToString()
                .Replace(KeyPrefix, "", StringComparison.Ordinal)
                .Replace(KeySuffix, "", StringComparison.Ordinal);

            var lastSeen = dict.TryGetValue("lastSeen", out var ts)
                && DateTimeOffset.TryParse(ts, out var parsed) ? parsed : DateTimeOffset.MinValue;

            var isAlive = (DateTimeOffset.UtcNow - lastSeen) < ZombieThreshold;

            var uptime = dict.TryGetValue("uptime", out var uptimeStr)
                && long.TryParse(uptimeStr, out var u) ? u : 0;

            players.Add(new PlayerStatus(
                playerId,
                lastSeen,
                dict.GetValueOrDefault("state", "Unknown")!,
                isAlive,
                uptime,
                dict.GetValueOrDefault("version")));
        }

        return players;
    }
}
