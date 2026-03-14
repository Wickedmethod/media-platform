using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;

namespace MediaPlatform.Application.Queries;

public sealed class GetQueueHandler(IQueueRepository repository)
{
    public async Task<List<QueueItem>> HandleAsync(CancellationToken ct = default)
    {
        return await repository.GetQueueAsync(ct);
    }
}
