using System.Globalization;
using System.Text.Json;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using StackExchange.Redis;

namespace MediaPlatform.Infrastructure.Redis;

public sealed class RedisSessionRepository(IConnectionMultiplexer redis) : ISessionRepository
{
    private const string SessionPrefix = "media:session:";
    private const string SessionQueuePrefix = "media:session-queue:";
    private const string SessionPlaybackPrefix = "media:session-playback:";
    private const string ActiveSessionsKey = "media:sessions:active";

    private IDatabase Db => redis.GetDatabase();

    public async Task<PlaybackSession?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var hash = await Db.HashGetAllAsync(SessionKey(sessionId));
        return hash.Length == 0 ? null : DeserializeSession(sessionId, hash);
    }

    public async Task<PlaybackSession?> GetPersonalSessionAsync(string userId, CancellationToken ct = default)
    {
        var members = await Db.SetMembersAsync(ActiveSessionsKey);
        foreach (var member in members)
        {
            var sid = member.ToString();
            if (sid.StartsWith(userId + ":", StringComparison.Ordinal))
            {
                return await GetSessionAsync(sid, ct);
            }
        }
        return null;
    }

    public async Task SaveSessionAsync(PlaybackSession session, CancellationToken ct = default)
    {
        var entries = new HashEntry[]
        {
            new("userId", session.UserId ?? ""),
            new("deviceId", session.DeviceId ?? ""),
            new("type", session.Type.ToString()),
            new("createdAt", session.CreatedAt.ToString("O")),
            new("lastActivityAt", session.LastActivityAt.ToString("O")),
        };

        var key = SessionKey(session.SessionId);
        await Db.HashSetAsync(key, entries);
        await Db.SetAddAsync(ActiveSessionsKey, session.SessionId);

        // Personal sessions expire after 4 hours of inactivity
        if (session.Type == SessionType.Personal)
        {
            await Db.KeyExpireAsync(key, TimeSpan.FromHours(4));
            await Db.KeyExpireAsync(SessionQueueKey(session.SessionId), TimeSpan.FromHours(4));
            await Db.KeyExpireAsync(SessionPlaybackKey(session.SessionId), TimeSpan.FromHours(4));
        }
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken ct = default)
    {
        await Db.KeyDeleteAsync(SessionKey(sessionId));
        await Db.KeyDeleteAsync(SessionQueueKey(sessionId));
        await Db.KeyDeleteAsync(SessionPlaybackKey(sessionId));
        await Db.SetRemoveAsync(ActiveSessionsKey, sessionId);
    }

    public async Task<List<PlaybackSession>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        var members = await Db.SetMembersAsync(ActiveSessionsKey);
        var sessions = new List<PlaybackSession>();
        foreach (var member in members)
        {
            var session = await GetSessionAsync(member.ToString(), ct);
            if (session is not null)
                sessions.Add(session);
            else
                await Db.SetRemoveAsync(ActiveSessionsKey, member); // cleanup stale entry
        }
        return sessions;
    }

    public async Task<List<QueueItem>> GetSessionQueueAsync(string sessionId, CancellationToken ct = default)
    {
        var values = await Db.ListRangeAsync(SessionQueueKey(sessionId));
        return values
            .Select(v => DeserializeItem(v!))
            .Where(item => item is not null)
            .ToList()!;
    }

    public async Task AddToSessionQueueAsync(string sessionId, QueueItem item, CancellationToken ct = default)
    {
        var json = SerializeItem(item);
        await Db.ListRightPushAsync(SessionQueueKey(sessionId), json);
    }

    public async Task<QueueItem?> DequeueNextFromSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var value = await Db.ListLeftPopAsync(SessionQueueKey(sessionId));
        return value.IsNullOrEmpty ? null : DeserializeItem(value!);
    }

    public async Task ClearSessionQueueAsync(string sessionId, CancellationToken ct = default)
    {
        await Db.KeyDeleteAsync(SessionQueueKey(sessionId));
    }

    public async Task SaveSessionPlaybackStateAsync(
        string sessionId,
        PlaybackState state,
        CancellationToken ct = default)
    {
        var entries = new HashEntry[]
        {
            new("state", state.State.ToString()),
            new("itemJson", state.CurrentItem is not null ? SerializeItem(state.CurrentItem) : ""),
            new("startedAt", state.StartedAt?.ToString("O") ?? ""),
            new("positionSeconds", state.PositionSeconds.ToString(CultureInfo.InvariantCulture)),
            new("retryCount", state.RetryCount.ToString()),
            new("lastError", state.LastError ?? ""),
        };

        await Db.HashSetAsync(SessionPlaybackKey(sessionId), entries);
    }

    public async Task<PlaybackState> GetSessionPlaybackStateAsync(string sessionId, CancellationToken ct = default)
    {
        var hash = await Db.HashGetAllAsync(SessionPlaybackKey(sessionId));
        if (hash.Length == 0)
            return new PlaybackState();

        var dict = hash.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

        var playerState = Enum.Parse<PlayerState>(dict.GetValueOrDefault("state", "Idle")!);
        QueueItem? currentItem = null;

        if (dict.TryGetValue("itemJson", out var itemJson) && !string.IsNullOrEmpty(itemJson))
            currentItem = DeserializeItem(itemJson);

        DateTimeOffset? startedAt = dict.TryGetValue("startedAt", out var startedStr)
            && DateTimeOffset.TryParse(startedStr, out var parsed)
                ? parsed
                : null;

        var positionSeconds = dict.TryGetValue("positionSeconds", out var posStr)
            && double.TryParse(posStr, CultureInfo.InvariantCulture, out var pos)
                ? pos
                : 0;

        var retryCount = dict.TryGetValue("retryCount", out var retryStr)
            && int.TryParse(retryStr, out var retry)
                ? retry
                : 0;

        dict.TryGetValue("lastError", out var lastError);

        return new PlaybackState(playerState, currentItem, startedAt, positionSeconds, retryCount, lastError);
    }

    private static string SessionKey(string sessionId) => $"{SessionPrefix}{sessionId}";
    private static string SessionQueueKey(string sessionId) => $"{SessionQueuePrefix}{sessionId}";
    private static string SessionPlaybackKey(string sessionId) => $"{SessionPlaybackPrefix}{sessionId}";

    private static PlaybackSession DeserializeSession(string sessionId, HashEntry[] hash)
    {
        var dict = hash.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

        var userId = dict.GetValueOrDefault("userId");
        if (string.IsNullOrEmpty(userId)) userId = null;

        var deviceId = dict.GetValueOrDefault("deviceId");
        if (string.IsNullOrEmpty(deviceId)) deviceId = null;

        var type = Enum.TryParse<SessionType>(dict.GetValueOrDefault("type", "Shared"), out var t)
            ? t
            : SessionType.Shared;

        var createdAt = DateTimeOffset.TryParse(dict.GetValueOrDefault("createdAt"), out var c)
            ? c
            : DateTimeOffset.UtcNow;

        var lastActivity = DateTimeOffset.TryParse(dict.GetValueOrDefault("lastActivityAt"), out var la)
            ? la
            : DateTimeOffset.UtcNow;

        return PlaybackSession.Restore(sessionId, userId, deviceId, type, new PlaybackState(), createdAt, lastActivity);
    }

    private static string SerializeItem(QueueItem item)
    {
        return JsonSerializer.Serialize(new QueueItemDto(
            item.Id, item.Url.Value, item.Title, item.Status.ToString(), item.AddedAt, item.StartAtSeconds,
            item.AddedByUserId, item.AddedByName, item.Channel, item.DurationSeconds, item.ThumbnailUrl));
    }

    private static QueueItem? DeserializeItem(string json)
    {
        var dto = JsonSerializer.Deserialize<QueueItemDto>(json);
        if (dto is null) return null;

        var url = VideoUrl.Create(dto.Url);
        var status = Enum.Parse<QueueItemStatus>(dto.Status);
        return new QueueItem(dto.Id, url, dto.Title, status, dto.AddedAt, dto.StartAtSeconds,
            dto.AddedByUserId, dto.AddedByName, dto.Channel, dto.DurationSeconds, dto.ThumbnailUrl);
    }

    private sealed record QueueItemDto(
        string Id,
        string Url,
        string Title,
        string Status,
        DateTimeOffset AddedAt,
        double StartAtSeconds = 0,
        string? AddedByUserId = null,
        string? AddedByName = null,
        string? Channel = null,
        int? DurationSeconds = null,
        string? ThumbnailUrl = null);
}
