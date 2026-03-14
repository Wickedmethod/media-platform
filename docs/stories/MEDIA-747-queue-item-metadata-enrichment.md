# MEDIA-747: Queue Item Metadata Enrichment (YouTube Title/Channel Fetch)

## Story

**Epic:** MEDIA-003 — Queue and Player API  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-710 (Invidious integration pattern)

---

## Summary

When a user adds a queue item by URL only, the API automatically fetches YouTube metadata (title, channel name, duration, thumbnail URL) via Invidious. This enriches queue items so the frontend can display full details without a separate metadata lookup.

---

## Flow

```
POST /queue/add { "url": "https://youtube.com/watch?v=dQw4w9WgXcQ" }
    │
    ▼
QueueService.AddAsync()
    │ extract videoId from URL
    ▼
IMetadataEnricher.EnrichAsync(videoId)
    │ GET https://invidious.example/api/v1/videos/{videoId}?fields=title,author,lengthSeconds,videoThumbnails
    ▼
QueueItem stored with:
    title: "Rick Astley - Never Gonna Give You Up"
    channel: "Rick Astley"
    durationSeconds: 212
    thumbnailUrl: "https://i.ytimg.com/vi/dQw4w9WgXcQ/mqdefault.jpg"
```

---

## Implementation

### Metadata Enricher Interface

```csharp
public interface IMetadataEnricher
{
    Task<VideoMetadata?> EnrichAsync(string videoId, CancellationToken ct = default);
}

public record VideoMetadata(
    string Title,
    string Channel,
    int DurationSeconds,
    string? ThumbnailUrl);
```

### Invidious Metadata Enricher

```csharp
public class InvidiousMetadataEnricher(
    IHttpClientFactory httpFactory,
    IOptions<InvidiousOptions> options,
    ILogger<InvidiousMetadataEnricher> logger) : IMetadataEnricher
{
    public async Task<VideoMetadata?> EnrichAsync(string videoId, CancellationToken ct)
    {
        using var client = httpFactory.CreateClient("Invidious");
        var url = $"{options.Value.BaseUrl}/api/v1/videos/{Uri.EscapeDataString(videoId)}" +
                  "?fields=title,author,lengthSeconds,videoThumbnails";

        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Invidious metadata fetch failed for {VideoId}: {Status}",
                videoId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<InvidiousVideoResponse>(ct);
        return json is null ? null : new VideoMetadata(
            json.Title,
            json.Author,
            json.LengthSeconds,
            json.VideoThumbnails?.FirstOrDefault(t => t.Quality == "medium")?.Url);
    }
}
```

### Queue Item Model Extension

```csharp
public record QueueItem
{
    // Existing
    public string Id { get; init; }
    public string Url { get; init; }
    // New — populated by enricher
    public string? Title { get; init; }
    public string? Channel { get; init; }
    public int? DurationSeconds { get; init; }
    public string? ThumbnailUrl { get; init; }
}
```

---

## Enrichment Strategy

| Scenario | Behavior |
|----------|----------|
| User provides URL only | Fetch metadata from Invidious |
| User provides URL + title | Keep user's title, fetch remaining fields |
| Invidious unavailable | Store item without metadata (graceful degradation) |
| Invalid videoId | Return 400 (after URL validation in MEDIA-749) |
| Timeout (>3s) | Skip enrichment, log warning |

### Caching

- Cache metadata in Redis for 24h keyed by `metadata:{videoId}`
- Avoids redundant Invidious calls for the same video added again

---

## Tasks

- [ ] Create `IMetadataEnricher` interface
- [ ] Implement `InvidiousMetadataEnricher`
- [ ] Add metadata fields to `QueueItem` model
- [ ] Call enricher in `QueueService.AddAsync` after URL validation
- [ ] Add Redis caching for metadata (24h TTL)
- [ ] Configure Invidious base URL via `InvidiousOptions`
- [ ] Handle timeout (3s) and fallback gracefully
- [ ] Unit tests for enricher (success, failure, timeout, cached)
- [ ] Integration test for enrichment in add flow

---

## Acceptance Criteria

- [ ] Adding a URL-only queue item auto-populates title, channel, duration
- [ ] Metadata is cached in Redis (24h TTL)
- [ ] Invidious failure doesn't block item add (graceful degradation)
- [ ] Enrichment timeout is 3s max
- [ ] Frontend displays enriched metadata in queue view
