using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;

namespace MediaPlatform.Domain.Entities;

public sealed class QueueItem
{
    public string Id { get; }
    public VideoUrl Url { get; }
    public string Title { get; private set; }
    public QueueItemStatus Status { get; private set; }
    public DateTimeOffset AddedAt { get; }
    public double StartAtSeconds { get; }
    public string? AddedByUserId { get; }
    public string? AddedByName { get; }
    public string? Channel { get; private set; }
    public int? DurationSeconds { get; private set; }
    public string? ThumbnailUrl { get; private set; }

    public QueueItem(string id, VideoUrl url, string title, double startAtSeconds = 0,
        string? addedByUserId = null, string? addedByName = null)
    {
        Id = id;
        Url = url;
        Title = title;
        Status = QueueItemStatus.Pending;
        AddedAt = DateTimeOffset.UtcNow;
        StartAtSeconds = startAtSeconds >= 0 ? startAtSeconds : 0;
        AddedByUserId = addedByUserId;
        AddedByName = addedByName;
    }

    public QueueItem(string id, VideoUrl url, string title, QueueItemStatus status,
        DateTimeOffset addedAt, double startAtSeconds = 0,
        string? addedByUserId = null, string? addedByName = null,
        string? channel = null, int? durationSeconds = null, string? thumbnailUrl = null)
    {
        Id = id;
        Url = url;
        Title = title;
        Status = status;
        AddedAt = addedAt;
        StartAtSeconds = startAtSeconds >= 0 ? startAtSeconds : 0;
        AddedByUserId = addedByUserId;
        AddedByName = addedByName;
        Channel = channel;
        DurationSeconds = durationSeconds;
        ThumbnailUrl = thumbnailUrl;
    }

    public void EnrichMetadata(string? title, string? channel, int? durationSeconds, string? thumbnailUrl)
    {
        if (!string.IsNullOrEmpty(title)) Title = title;
        Channel = channel;
        DurationSeconds = durationSeconds;
        ThumbnailUrl = thumbnailUrl;
    }

    public void MarkPlaying() => Status = QueueItemStatus.Playing;
    public void MarkPlayed() => Status = QueueItemStatus.Played;
    public void MarkFailed() => Status = QueueItemStatus.Failed;
    public void MarkRemoved() => Status = QueueItemStatus.Removed;
}
