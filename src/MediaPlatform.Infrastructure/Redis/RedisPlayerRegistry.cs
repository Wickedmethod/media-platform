using System.Text.Json;
using MediaPlatform.Application.Abstractions;
using StackExchange.Redis;

namespace MediaPlatform.Infrastructure.Redis;

public sealed class RedisPlayerRegistry(IConnectionMultiplexer redis) : IPlayerRegistry
{
    private const string HeartbeatPrefix = "player:";
    private const string HeartbeatSuffix = ":heartbeat";
    private const string RegistrationPrefix = "worker:";
    private static readonly TimeSpan HeartbeatTtl = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan ZombieThreshold = TimeSpan.FromSeconds(90);

    private IDatabase Db => redis.GetDatabase();
    private IServer Server => redis.GetServers()[0];

    public async Task RecordHeartbeatAsync(PlayerHeartbeat heartbeat, CancellationToken ct = default)
    {
        var key = $"{HeartbeatPrefix}{heartbeat.PlayerId}{HeartbeatSuffix}";
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

    public async Task<WorkerRegistrationResult> RegisterAsync(WorkerRegistration registration, CancellationToken ct = default)
    {
        var playerId = GeneratePlayerId(registration.Name);
        var key = $"{RegistrationPrefix}{playerId}";

        var entries = new HashEntry[]
        {
            new("name", registration.Name),
            new("registeredAt", DateTimeOffset.UtcNow.ToString("O")),
            new("version", registration.Version ?? ""),
            new("os", registration.Os ?? ""),
            new("capabilities", registration.Capabilities is not null
                ? JsonSerializer.Serialize(registration.Capabilities)
                : "")
        };

        await Db.HashSetAsync(key, entries);
        // No TTL — registration is persistent

        return new WorkerRegistrationResult(
            playerId,
            DateTimeOffset.UtcNow,
            new WorkerConfig());
    }

    public async Task<IReadOnlyList<PlayerStatus>> GetAllPlayersAsync(CancellationToken ct = default)
    {
        // Collect registrations first
        var registrations = new Dictionary<string, (string? Name, DateTimeOffset? RegisteredAt, WorkerCapabilities? Caps)>();
        var regKeys = Server.Keys(pattern: $"{RegistrationPrefix}*");
        foreach (var key in regKeys)
        {
            var hash = await Db.HashGetAllAsync(key);
            if (hash.Length == 0) continue;

            var dict = hash.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
            var id = key.ToString().Replace(RegistrationPrefix, "", StringComparison.Ordinal);

            var registeredAt = dict.TryGetValue("registeredAt", out var regTs)
                && DateTimeOffset.TryParse(regTs, out var regParsed) ? regParsed : (DateTimeOffset?)null;

            WorkerCapabilities? caps = null;
            if (dict.TryGetValue("capabilities", out var capsJson) && !string.IsNullOrEmpty(capsJson))
            {
                caps = JsonSerializer.Deserialize<WorkerCapabilities>(capsJson);
            }

            registrations[id] = (dict.GetValueOrDefault("name"), registeredAt, caps);
        }

        // Collect heartbeats
        var heartbeatKeys = Server.Keys(pattern: $"{HeartbeatPrefix}*{HeartbeatSuffix}");
        var players = new Dictionary<string, PlayerStatus>();

        foreach (var key in heartbeatKeys)
        {
            var hash = await Db.HashGetAllAsync(key);
            if (hash.Length == 0) continue;

            var dict = hash.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

            var playerId = key.ToString()
                .Replace(HeartbeatPrefix, "", StringComparison.Ordinal)
                .Replace(HeartbeatSuffix, "", StringComparison.Ordinal);

            var lastSeen = dict.TryGetValue("lastSeen", out var ts)
                && DateTimeOffset.TryParse(ts, out var parsed) ? parsed : DateTimeOffset.MinValue;

            var isAlive = (DateTimeOffset.UtcNow - lastSeen) < ZombieThreshold;

            var uptime = dict.TryGetValue("uptime", out var uptimeStr)
                && long.TryParse(uptimeStr, out var u) ? u : 0;

            var reg = registrations.GetValueOrDefault(playerId);

            players[playerId] = new PlayerStatus(
                playerId,
                lastSeen,
                dict.GetValueOrDefault("state", "Unknown")!,
                isAlive,
                uptime,
                dict.GetValueOrDefault("version"),
                reg.Name,
                reg.Caps,
                reg.RegisteredAt);
        }

        // Add registered players that have no heartbeat yet
        foreach (var (id, reg) in registrations)
        {
            if (!players.ContainsKey(id))
            {
                players[id] = new PlayerStatus(
                    id,
                    DateTimeOffset.MinValue,
                    "Unknown",
                    false,
                    0,
                    null,
                    reg.Name,
                    reg.Caps,
                    reg.RegisteredAt);
            }
        }

        return players.Values.ToList();
    }

    private static string GeneratePlayerId(string name)
    {
        return name.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("'", "", StringComparison.Ordinal)
            .Replace("\"", "", StringComparison.Ordinal);
    }
}
