using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Validation;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.ValueObjects;

namespace MediaPlatform.Application.Commands;

public sealed record AddToQueueCommand(string Url, string Title, double StartAtSeconds = 0,
    string? AddedByUserId = null, string? AddedByName = null);

public sealed class AddToQueueHandler(IQueueRepository repository, IMetadataEnricher metadataEnricher)
{
    public async Task<QueueItem> HandleAsync(AddToQueueCommand command, CancellationToken ct = default)
    {
        var videoUrl = VideoUrl.Create(command.Url);
        var item = new QueueItem(Guid.NewGuid().ToString("N"), videoUrl, command.Title, command.StartAtSeconds,
            command.AddedByUserId, command.AddedByName);

        // Enrich with YouTube metadata (best-effort, non-blocking on failure)
        var videoId = QueueItemValidator.ExtractVideoId(command.Url);
        if (videoId is not null)
        {
            var metadata = await metadataEnricher.EnrichAsync(videoId, ct);
            if (metadata is not null)
            {
                item.EnrichMetadata(metadata.Title, metadata.Channel, metadata.DurationSeconds, metadata.ThumbnailUrl);
            }
        }

        await repository.AddAsync(item, ct);
        return item;
    }
}
