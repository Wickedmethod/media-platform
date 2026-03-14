# MEDIA-749: Queue Item Validation & Sanitization

## Story

**Epic:** MEDIA-003 — Queue and Player API  
**Priority:** High  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** None (existing API)

---

## Summary

Validate and sanitize all queue item input before storing. Prevents malformed URLs, XSS payloads in titles, overly long strings, and non-YouTube URLs from entering the queue. Works as the first gate in the `POST /queue/add` pipeline.

---

## Validation Rules

| Field | Rule | Error |
|-------|------|-------|
| `url` | Required | 400: "URL is required" |
| `url` | Must be valid URI | 400: "Invalid URL format" |
| `url` | Must match YouTube URL pattern | 400: "Only YouTube URLs are supported" |
| `url` | Must contain extractable videoId | 400: "Could not extract video ID from URL" |
| `url` | Max 2048 chars | 400: "URL exceeds maximum length" |
| `title` | Optional, max 200 chars | 400: "Title exceeds 200 characters" |
| `title` | HTML stripped | (silent sanitization) |

### YouTube URL Patterns

```csharp
private static readonly Regex[] YouTubePatterns =
[
    new(@"^https?://(?:www\.)?youtube\.com/watch\?v=(?<id>[\w-]{11})", RegexOptions.Compiled),
    new(@"^https?://youtu\.be/(?<id>[\w-]{11})", RegexOptions.Compiled),
    new(@"^https?://(?:www\.)?youtube\.com/embed/(?<id>[\w-]{11})", RegexOptions.Compiled),
    new(@"^https?://music\.youtube\.com/watch\?v=(?<id>[\w-]{11})", RegexOptions.Compiled),
];
```

---

## Implementation

### Validator

```csharp
public static class QueueItemValidator
{
    public static ValidationResult Validate(AddQueueItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return ValidationResult.Fail("URL is required");

        if (request.Url.Length > 2048)
            return ValidationResult.Fail("URL exceeds maximum length");

        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "https" && uri.Scheme != "http"))
            return ValidationResult.Fail("Invalid URL format");

        var videoId = ExtractVideoId(request.Url);
        if (videoId is null)
            return ValidationResult.Fail("Only YouTube URLs are supported");

        if (request.Title is { Length: > 200 })
            return ValidationResult.Fail("Title exceeds 200 characters");

        return ValidationResult.Ok(videoId);
    }

    public static string? ExtractVideoId(string url)
    {
        foreach (var pattern in YouTubePatterns)
        {
            var match = pattern.Match(url);
            if (match.Success) return match.Groups["id"].Value;
        }
        return null;
    }
}
```

### Sanitizer

```csharp
public static class QueueItemSanitizer
{
    public static AddQueueItemRequest Sanitize(AddQueueItemRequest request)
    {
        return request with
        {
            Url = request.Url.Trim(),
            Title = request.Title is not null
                ? StripHtml(request.Title.Trim())
                : null,
        };
    }

    private static string StripHtml(string input)
    {
        // Remove all HTML tags
        return Regex.Replace(input, "<[^>]*>", string.Empty);
    }
}
```

### Pipeline

```csharp
app.MapPost("/queue/add", async (AddQueueItemRequest request, QueueService queue) =>
{
    var sanitized = QueueItemSanitizer.Sanitize(request);
    var validation = QueueItemValidator.Validate(sanitized);

    if (!validation.IsValid)
        return Results.BadRequest(new ApiError("validation_error", validation.Error!));

    var item = await queue.AddAsync(sanitized, validation.VideoId!);
    return Results.Created($"/queue/{item.Id}", item);
});
```

---

## Tasks

- [ ] Create `QueueItemValidator` with URL + title validation
- [ ] Create `QueueItemSanitizer` with HTML stripping + trimming
- [ ] Extract `ExtractVideoId` as shared utility (reused by enricher)
- [ ] Wire validation into `POST /queue/add` endpoint
- [ ] Return `ApiError` format (consistent with MEDIA-620)
- [ ] Unit tests for every validation rule (valid, invalid, edge cases)
- [ ] Unit tests for sanitizer (XSS payloads, whitespace, long strings)
- [ ] Unit tests for all YouTube URL patterns

---

## Acceptance Criteria

- [ ] Only YouTube URLs accepted (youtube.com, youtu.be, music.youtube.com)
- [ ] Non-YouTube URLs rejected with 400
- [ ] HTML tags stripped from title input
- [ ] URLs over 2048 chars rejected
- [ ] Titles over 200 chars rejected
- [ ] 11-char videoId extracted from all supported URL formats
- [ ] Error responses follow `ApiError` format
