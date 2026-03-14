using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Application.Abstractions;

public interface IQueueRepository
{
    Task<List<QueueItem>> GetQueueAsync(CancellationToken ct = default);
    Task AddAsync(QueueItem item, CancellationToken ct = default);
    Task AddNextAsync(QueueItem item, CancellationToken ct = default);
    Task RemoveAsync(string itemId, CancellationToken ct = default);
    Task<QueueItem?> DequeueNextAsync(CancellationToken ct = default);
    Task<QueueItem?> DequeueShuffledAsync(CancellationToken ct = default);
    Task<PlaybackState> GetPlaybackStateAsync(CancellationToken ct = default);
    Task SavePlaybackStateAsync(PlaybackState state, CancellationToken ct = default);
    Task<QueueMode> GetQueueModeAsync(CancellationToken ct = default);
    Task SetQueueModeAsync(QueueMode mode, CancellationToken ct = default);
}
