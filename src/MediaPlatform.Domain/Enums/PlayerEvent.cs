namespace MediaPlatform.Domain.Enums;

public enum PlayerEvent
{
    PlayRequested,
    PauseRequested,
    SkipRequested,
    StopRequested,
    BufferingStarted,
    PlaybackStarted,
    TrackEnded,
    ErrorOccurred
}
