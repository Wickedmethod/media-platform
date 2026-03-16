using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Domain.Entities;

public sealed class PlaybackSession
{
    public string SessionId { get; }
    public string? UserId { get; }
    public string? DeviceId { get; }
    public SessionType Type { get; }
    public PlaybackState Playback { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>
    /// The shared session singleton ID.
    /// </summary>
    public const string SharedSessionId = "shared";

    private PlaybackSession(
        string sessionId,
        string? userId,
        string? deviceId,
        SessionType type,
        PlaybackState playback,
        DateTimeOffset createdAt,
        DateTimeOffset lastActivityAt)
    {
        SessionId = sessionId;
        UserId = userId;
        DeviceId = deviceId;
        Type = type;
        Playback = playback;
        CreatedAt = createdAt;
        LastActivityAt = lastActivityAt;
    }

    public static PlaybackSession CreatePersonal(string userId, string deviceId)
    {
        var sessionId = $"{userId}:{deviceId}";
        return new PlaybackSession(
            sessionId,
            userId,
            deviceId,
            SessionType.Personal,
            new PlaybackState(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    public static PlaybackSession CreateShared()
    {
        return new PlaybackSession(
            SharedSessionId,
            null,
            null,
            SessionType.Shared,
            new PlaybackState(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    public static PlaybackSession Restore(
        string sessionId,
        string? userId,
        string? deviceId,
        SessionType type,
        PlaybackState playback,
        DateTimeOffset createdAt,
        DateTimeOffset lastActivityAt)
    {
        return new PlaybackSession(sessionId, userId, deviceId, type, playback, createdAt, lastActivityAt);
    }

    public void Touch() => LastActivityAt = DateTimeOffset.UtcNow;

    public bool IsExpired(TimeSpan maxIdle) => DateTimeOffset.UtcNow - LastActivityAt > maxIdle;
}
