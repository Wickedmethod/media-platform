using MediaPlatform.Domain.Entities;

namespace MediaPlatform.Application.Abstractions;

public interface ISessionRepository
{
    Task<PlaybackSession?> GetSessionAsync(string sessionId, CancellationToken ct = default);
    Task<PlaybackSession?> GetPersonalSessionAsync(string userId, CancellationToken ct = default);
    Task SaveSessionAsync(PlaybackSession session, CancellationToken ct = default);
    Task DeleteSessionAsync(string sessionId, CancellationToken ct = default);
    Task<List<PlaybackSession>> GetActiveSessionsAsync(CancellationToken ct = default);

    Task<List<QueueItem>> GetSessionQueueAsync(string sessionId, CancellationToken ct = default);
    Task AddToSessionQueueAsync(string sessionId, QueueItem item, CancellationToken ct = default);
    Task<QueueItem?> DequeueNextFromSessionAsync(string sessionId, CancellationToken ct = default);
    Task ClearSessionQueueAsync(string sessionId, CancellationToken ct = default);

    Task SaveSessionPlaybackStateAsync(string sessionId, PlaybackState state, CancellationToken ct = default);
    Task<PlaybackState> GetSessionPlaybackStateAsync(string sessionId, CancellationToken ct = default);
}
