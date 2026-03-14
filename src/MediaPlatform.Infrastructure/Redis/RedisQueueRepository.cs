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

    private IDatabase Db => redis.GetDatabase();

    public async Task<List<QueueItem>> GetQueueAsync(CancellationToken ct = default)
    {
        var values = await Db.ListRangeAsync(QueueKey);
        return values
            .Select(v => Deserialize(v!))
            .Where(item => item is not null)
            .ToList()!;
    }

    public async Task AddAsync(QueueItem item, CancellationToken ct = default)
    {
        var json = Serialize(item);
        await Db.ListRightPushAsync(QueueKey, json);
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

        return new PlaybackState(playerState, currentItem, startedAt);
    }

    public async Task SavePlaybackStateAsync(PlaybackState state, CancellationToken ct = default)
    {
        var entries = new HashEntry[]
        {
            new("state", state.State.ToString()),
            new("itemJson", state.CurrentItem is not null ? Serialize(state.CurrentItem) : ""),
            new("startedAt", state.StartedAt?.ToString("O") ?? "")
        };

        await Db.HashSetAsync(NowPlayingKey, entries);
    }

    private static string Serialize(QueueItem item)
    {
        return JsonSerializer.Serialize(new QueueItemDto(
            item.Id, item.Url.Value, item.Title, item.Status.ToString(), item.AddedAt));
    }

    private static QueueItem? Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<QueueItemDto>(json);
        if (dto is null) return null;

        var url = VideoUrl.Create(dto.Url);
        var status = Enum.Parse<QueueItemStatus>(dto.Status);
        return new QueueItem(dto.Id, url, dto.Title, status, dto.AddedAt);
    }

    private sealed record QueueItemDto(
        string Id, string Url, string Title, string Status, DateTimeOffset AddedAt);
}
