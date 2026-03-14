using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Domain.Entities;

public sealed class PlaybackState
{
    public PlayerState State { get; private set; }
    public QueueItem? CurrentItem { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public double PositionSeconds { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }

    public PlaybackState()
    {
        State = PlayerState.Idle;
    }

    public PlaybackState(PlayerState state, QueueItem? currentItem, DateTimeOffset? startedAt,
        double positionSeconds = 0, int retryCount = 0, string? lastError = null)
    {
        State = state;
        CurrentItem = currentItem;
        StartedAt = startedAt;
        PositionSeconds = positionSeconds;
        RetryCount = retryCount;
        LastError = lastError;
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
        PositionSeconds = 0;
        ResetRetry();
    }

    public void SetError(string? reason = null)
    {
        CurrentItem?.MarkFailed();
        State = PlayerState.Error;
        LastError = reason;
    }

    public void SetStopped()
    {
        State = PlayerState.Stopped;
        CurrentItem = null;
        StartedAt = null;
        PositionSeconds = 0;
        ResetRetry();
    }

    public void UpdatePosition(double seconds)
    {
        if (seconds >= 0)
            PositionSeconds = seconds;
    }

    public void IncrementRetry() => RetryCount++;
    public void ResetRetry() { RetryCount = 0; LastError = null; }
}
