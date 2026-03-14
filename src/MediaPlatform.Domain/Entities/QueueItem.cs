using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;

namespace MediaPlatform.Domain.Entities;

public sealed class QueueItem
{
    public string Id { get; }
    public VideoUrl Url { get; }
    public string Title { get; }
    public QueueItemStatus Status { get; private set; }
    public DateTimeOffset AddedAt { get; }
    public double StartAtSeconds { get; }

    public QueueItem(string id, VideoUrl url, string title, double startAtSeconds = 0)
    {
        Id = id;
        Url = url;
        Title = title;
        Status = QueueItemStatus.Pending;
        AddedAt = DateTimeOffset.UtcNow;
        StartAtSeconds = startAtSeconds >= 0 ? startAtSeconds : 0;
    }

    public QueueItem(string id, VideoUrl url, string title, QueueItemStatus status,
        DateTimeOffset addedAt, double startAtSeconds = 0)
    {
        Id = id;
        Url = url;
        Title = title;
        Status = status;
        AddedAt = addedAt;
        StartAtSeconds = startAtSeconds >= 0 ? startAtSeconds : 0;
    }

    public void MarkPlaying() => Status = QueueItemStatus.Playing;
    public void MarkPlayed() => Status = QueueItemStatus.Played;
    public void MarkFailed() => Status = QueueItemStatus.Failed;
    public void MarkRemoved() => Status = QueueItemStatus.Removed;
}
