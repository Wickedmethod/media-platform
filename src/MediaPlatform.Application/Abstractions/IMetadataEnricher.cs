namespace MediaPlatform.Application.Abstractions;

public record VideoMetadata(
    string Title,
    string Channel,
    int DurationSeconds,
    string? ThumbnailUrl);

public interface IMetadataEnricher
{
    Task<VideoMetadata?> EnrichAsync(string videoId, CancellationToken ct = default);
}
