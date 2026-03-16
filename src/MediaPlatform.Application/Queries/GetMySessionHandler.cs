using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;

namespace MediaPlatform.Application.Queries;

public sealed record GetMySessionQuery(string UserId);

public sealed record SessionSnapshot(
    PlaybackSession Session,
    List<QueueItem> Queue,
    PlaybackState Playback);

public sealed class GetMySessionHandler(ISessionRepository sessions)
{
    public async Task<SessionSnapshot?> HandleAsync(GetMySessionQuery query, CancellationToken ct = default)
    {
        var session = await sessions.GetPersonalSessionAsync(query.UserId, ct);
        if (session is null)
            return null;

        var queue = await sessions.GetSessionQueueAsync(session.SessionId, ct);
        var playback = await sessions.GetSessionPlaybackStateAsync(session.SessionId, ct);

        return new SessionSnapshot(session, queue, playback);
    }
}
