using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Domain.Entities;

public sealed class PlaybackState
{
    public PlayerState State { get; private set; }
    public QueueItem? CurrentItem { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }

    public PlaybackState()
    {
        State = PlayerState.Idle;
    }

    public PlaybackState(PlayerState state, QueueItem? currentItem, DateTimeOffset? startedAt)
    {
        State = state;
        CurrentItem = currentItem;
        StartedAt = startedAt;
    }

    public void SetPlaying(QueueItem item)
    {
        State = PlayerState.Playing;
        CurrentItem = item;
        StartedAt = DateTimeOffset.UtcNow;
        item.MarkPlaying();
    }

    public void SetPaused()
    {
        State = PlayerState.Paused;
    }

    public void SetBuffering(QueueItem item)
    {
        State = PlayerState.Buffering;
        CurrentItem = item;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void SetIdle()
    {
        CurrentItem?.MarkPlayed();
        State = PlayerState.Idle;
        CurrentItem = null;
        StartedAt = null;
    }

    public void SetError()
    {
        CurrentItem?.MarkFailed();
        State = PlayerState.Error;
    }

    public void SetStopped()
    {
        State = PlayerState.Stopped;
        CurrentItem = null;
        StartedAt = null;
    }
}
