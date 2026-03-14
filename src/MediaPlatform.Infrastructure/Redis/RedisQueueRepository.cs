using System.Text.Json;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using StackExchange.Redis;

namespace MediaPlatform.Infrastructure.Redis;

public sealed class RedisQueueRepository(IConnectionMultiplexer redis) : IQueueRepository
{
    private const string QueueKey = "media:queue";
    private const string NowPlayingKey = "media:now-playing";
    private const string QueueModeKey = "media:queue-mode";

    private IDatabase Db => redis.GetDatabase();

    public async Task<List<QueueItem>> GetQueueAsync(CancellationToken ct = default)
    {
        var values = await Db.ListRangeAsync(QueueKey);
        return values
            .Select(v => Deserialize(v!))
            .Where(item => item is not null)
            .ToList()!;
    }

    public async Task<QueueItem?> GetByIdAsync(string itemId, CancellationToken ct = default)
    {
        var values = await Db.ListRangeAsync(QueueKey);
        foreach (var value in values)
        {
            var item = Deserialize(value!);
            if (item?.Id == itemId)
                return item;
        }
        return null;
    }

    public async Task AddAsync(QueueItem item, CancellationToken ct = default)
    {
        var json = Serialize(item);
        await Db.ListRightPushAsync(QueueKey, json);
    }

    public async Task AddNextAsync(QueueItem item, CancellationToken ct = default)
    {
        var json = Serialize(item);
        await Db.ListLeftPushAsync(QueueKey, json);
    }

    public async Task RemoveAsync(string itemId, CancellationToken ct = default)
    {
        var values = await Db.ListRangeAsync(QueueKey);
        foreach (var value in values)
        {
            var item = Deserialize(value!);
            if (item?.Id == itemId)
            {
                await Db.ListRemoveAsync(QueueKey, value);
                return;
            }
        }
    }

    public async Task<QueueItem?> DequeueNextAsync(CancellationToken ct = default)
    {
        var value = await Db.ListLeftPopAsync(QueueKey);
        return value.IsNullOrEmpty ? null : Deserialize(value!);
    }

    public async Task<QueueItem?> DequeueShuffledAsync(CancellationToken ct = default)
    {
        var length = await Db.ListLengthAsync(QueueKey);
        if (length == 0) return null;

        var index = Random.Shared.Next((int)length);
        var value = await Db.ListGetByIndexAsync(QueueKey, index);
        if (value.IsNullOrEmpty) return await DequeueNextAsync(ct);

        await Db.ListRemoveAsync(QueueKey, value, 1);
        return Deserialize(value!);
    }

    public async Task<PlaybackState> GetPlaybackStateAsync(CancellationToken ct = default)
    {
        var hash = await Db.HashGetAllAsync(NowPlayingKey);
        if (hash.Length == 0)
            return new PlaybackState();

        var dict = hash.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

        var playerState = Enum.Parse<PlayerState>(dict.GetValueOrDefault("state", "Idle")!);
        QueueItem? currentItem = null;

        if (dict.TryGetValue("itemJson", out var itemJson) && !string.IsNullOrEmpty(itemJson))
            currentItem = Deserialize(itemJson);

        DateTimeOffset? startedAt = dict.TryGetValue("startedAt", out var startedStr)
            && DateTimeOffset.TryParse(startedStr, out var parsed)
                ? parsed
                : null;

        var positionSeconds = dict.TryGetValue("positionSeconds", out var posStr)
            && double.TryParse(posStr, System.Globalization.CultureInfo.InvariantCulture, out var pos)
                ? pos
                : 0;

        var retryCount = dict.TryGetValue("retryCount", out var retryStr)
            && int.TryParse(retryStr, out var retry)
                ? retry
                : 0;

        dict.TryGetValue("lastError", out var lastError);

        return new PlaybackState(playerState, currentItem, startedAt, positionSeconds, retryCount, lastError);
    }

    public async Task SavePlaybackStateAsync(PlaybackState state, CancellationToken ct = default)
    {
        var entries = new HashEntry[]
        {
            new("state", state.State.ToString()),
            new("itemJson", state.CurrentItem is not null ? Serialize(state.CurrentItem) : ""),
            new("startedAt", state.StartedAt?.ToString("O") ?? ""),
            new("positionSeconds", state.PositionSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("retryCount", state.RetryCount.ToString()),
            new("lastError", state.LastError ?? "")
        };

        await Db.HashSetAsync(NowPlayingKey, entries);
    }

    public async Task<QueueMode> GetQueueModeAsync(CancellationToken ct = default)
    {
        var value = await Db.StringGetAsync(QueueModeKey);
        return value.IsNullOrEmpty
            ? QueueMode.Normal
            : Enum.TryParse<QueueMode>(value!, out var mode) ? mode : QueueMode.Normal;
    }

    public async Task SetQueueModeAsync(QueueMode mode, CancellationToken ct = default)
    {
        await Db.StringSetAsync(QueueModeKey, mode.ToString());
    }

    private static string Serialize(QueueItem item)
    {
        return JsonSerializer.Serialize(new QueueItemDto(
            item.Id, item.Url.Value, item.Title, item.Status.ToString(), item.AddedAt, item.StartAtSeconds,
            item.AddedByUserId, item.AddedByName));
    }

    private static QueueItem? Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<QueueItemDto>(json);
        if (dto is null) return null;

        var url = VideoUrl.Create(dto.Url);
        var status = Enum.Parse<QueueItemStatus>(dto.Status);
        return new QueueItem(dto.Id, url, dto.Title, status, dto.AddedAt, dto.StartAtSeconds,
            dto.AddedByUserId, dto.AddedByName);
    }

    private sealed record QueueItemDto(
        string Id, string Url, string Title, string Status, DateTimeOffset AddedAt, double StartAtSeconds = 0,
        string? AddedByUserId = null, string? AddedByName = null);
}
