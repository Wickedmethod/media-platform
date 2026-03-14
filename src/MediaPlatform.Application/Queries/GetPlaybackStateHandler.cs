using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;

namespace MediaPlatform.Application.Queries;

public sealed class GetPlaybackStateHandler(IQueueRepository repository)
{
    public async Task<PlaybackState> HandleAsync(CancellationToken ct = default)
    {
        return await repository.GetPlaybackStateAsync(ct);
    }
}
