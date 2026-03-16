using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MediaPlatform.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MediaPlatform.Infrastructure.Metadata;

public sealed class YouTubeMetadataEnricher(
    IHttpClientFactory httpFactory,
    IConnectionMultiplexer redis,
    IConfiguration config,
    ILogger<YouTubeMetadataEnricher> logger) : IMetadataEnricher
{
    private const string CachePrefix = "metadata:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<VideoMetadata?> EnrichAsync(string videoId, CancellationToken ct = default)
    {
        // Check cache first
        var cached = await GetCachedAsync(videoId);
        if (cached is not null) return cached;

        var apiKey = config.GetValue<string>("YouTube:ApiKey");
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogDebug("YouTube API key not configured, skipping metadata enrichment");
            return null;
        }

        try
        {
            var client = httpFactory.CreateClient("youtube");
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"https://www.googleapis.com/youtube/v3/videos?part=snippet,contentDetails&id={Uri.EscapeDataString(videoId)}&key={apiKey}";
            var response = await client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("YouTube metadata fetch failed for {VideoId}: {Status}", videoId, response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<YouTubeVideoListResponse>(ct);
            var item = data?.Items?.FirstOrDefault();
            if (item is null) return null;

            var duration = ParseIsoDuration(item.ContentDetails?.Duration);
            var thumbnail = item.Snippet?.Thumbnails?.Medium?.Url
                ?? item.Snippet?.Thumbnails?.Default?.Url
                ?? $"https://i.ytimg.com/vi/{videoId}/mqdefault.jpg";

            var metadata = new VideoMetadata(
                item.Snippet?.Title ?? videoId,
                item.Snippet?.ChannelTitle ?? "Unknown",
                duration,
                thumbnail);

            await CacheAsync(videoId, metadata);
            return metadata;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "YouTube metadata enrichment failed for {VideoId}", videoId);
            return null;
        }
    }

    private async Task<VideoMetadata?> GetCachedAsync(string videoId)
    {
        try
        {
            var db = redis.GetDatabase();
            var hash = await db.HashGetAllAsync($"{CachePrefix}{videoId}");
            if (hash.Length == 0) return null;

            var dict = hash.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
            return new VideoMetadata(
                dict.GetValueOrDefault("title", videoId)!,
                dict.GetValueOrDefault("channel", "Unknown")!,
                int.TryParse(dict.GetValueOrDefault("duration", "0"), out var d) ? d : 0,
                dict.GetValueOrDefault("thumbnail"));
        }
        catch
        {
            return null;
        }
    }

    private async Task CacheAsync(string videoId, VideoMetadata metadata)
    {
        try
        {
            var db = redis.GetDatabase();
            var key = $"{CachePrefix}{videoId}";
            await db.HashSetAsync(key, [
                new HashEntry("title", metadata.Title),
                new HashEntry("channel", metadata.Channel),
                new HashEntry("duration", metadata.DurationSeconds.ToString()),
                new HashEntry("thumbnail", metadata.ThumbnailUrl ?? "")
            ]);
            await db.KeyExpireAsync(key, CacheTtl);
        }
        catch
        {
            // cache write is best-effort
        }
    }

    private static int ParseIsoDuration(string? iso)
    {
        if (string.IsNullOrEmpty(iso)) return 0;
        var span = System.Xml.XmlConvert.ToTimeSpan(iso);
        return (int)span.TotalSeconds;
    }
}

// ── YouTube API response models ─────────────────────────────

internal record YouTubeVideoListResponse(
    [property: JsonPropertyName("items")] YouTubeVideoItem[]? Items);

internal record YouTubeVideoItem(
    [property: JsonPropertyName("snippet")] YouTubeVideoSnippet? Snippet,
    [property: JsonPropertyName("contentDetails")] YouTubeVideoContentDetails? ContentDetails);

internal record YouTubeVideoSnippet(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("channelTitle")] string? ChannelTitle,
    [property: JsonPropertyName("thumbnails")] YouTubeVideoThumbnails? Thumbnails);

internal record YouTubeVideoThumbnails(
    [property: JsonPropertyName("default")] YouTubeVideoThumbnail? Default,
    [property: JsonPropertyName("medium")] YouTubeVideoThumbnail? Medium);

internal record YouTubeVideoThumbnail(
    [property: JsonPropertyName("url")] string Url);

internal record YouTubeVideoContentDetails(
    [property: JsonPropertyName("duration")] string? Duration);
