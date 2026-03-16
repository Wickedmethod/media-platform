using MediaPlatform.Application.Abstractions;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.ValueObjects;

namespace MediaPlatform.Application.Commands;

public sealed record AddToSessionQueueCommand(
    string SessionId,
    string Url,
    string Title,
    double StartAtSeconds = 0,
    string? AddedByUserId = null,
    string? AddedByName = null);

public sealed class AddToSessionQueueHandler(ISessionRepository sessions, IMetadataEnricher metadataEnricher)
{
    public async Task<QueueItem> HandleAsync(AddToSessionQueueCommand command, CancellationToken ct = default)
    {
        var session = await sessions.GetSessionAsync(command.SessionId, ct)
            ?? throw new InvalidOperationException("Session not found");

        var videoUrl = VideoUrl.Create(command.Url);
        var item = new QueueItem(
            Guid.NewGuid().ToString("N"),
            videoUrl,
            command.Title,
            command.StartAtSeconds,
            command.AddedByUserId,
            command.AddedByName);

        // Enrich metadata (best-effort)
        var videoId = Validation.QueueItemValidator.ExtractVideoId(command.Url);
        if (videoId is not null)
        {
            var metadata = await metadataEnricher.EnrichAsync(videoId, ct);
            if (metadata is not null)
                item.EnrichMetadata(metadata.Title, metadata.Channel, metadata.DurationSeconds, metadata.ThumbnailUrl);
        }

        await sessions.AddToSessionQueueAsync(command.SessionId, item, ct);

        session.Touch();
        await sessions.SaveSessionAsync(session, ct);

        return item;
    }
}
