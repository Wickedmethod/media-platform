using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MediaPlatform.Api.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/search").WithTags("Search").RequireRateLimiting("general");

        group.MapGet("/youtube", SearchYouTubeAsync)
            .WithName("SearchYouTube")
            .Produces<IEnumerable<YouTubeSearchResult>>()
            .WithDescription("Search YouTube videos (uses YouTube Data API if configured, Piped fallback otherwise)");
    }

    private static async Task<IResult> SearchYouTubeAsync(
        string q,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length > 200)
            return Results.BadRequest(new ApiError("Invalid query"));

        // Try YouTube Data API v3 first (if key configured)
        var ytApiKey = config.GetValue<string>("YouTube:ApiKey");
        if (!string.IsNullOrEmpty(ytApiKey))
        {
            var result = await SearchViaYouTubeApi(q, ytApiKey, httpFactory, ct);
            if (result is not null) return Results.Ok(result);
        }

        // Fallback: Piped API instances
        var result2 = await SearchViaPiped(q, httpFactory, ct);
        if (result2 is not null) return Results.Ok(result2);

        return Results.StatusCode(502);
    }

    // ── YouTube Data API v3 ────────────────────────────────────

    private static async Task<List<YouTubeSearchResult>?> SearchViaYouTubeApi(
        string query, string apiKey, IHttpClientFactory httpFactory, CancellationToken ct)
    {
        var client = httpFactory.CreateClient("youtube");
        client.Timeout = TimeSpan.FromSeconds(10);

        try
        {
            var url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=20&q={Uri.EscapeDataString(query)}&key={apiKey}";
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadFromJsonAsync<YouTubeApiSearchResponse>(ct);
            if (data?.Items is null) return null;

            // We need video durations from a separate call
            var videoIds = string.Join(",", data.Items.Select(i => i.Id.VideoId).Where(id => !string.IsNullOrEmpty(id)));
            var durations = await GetVideoDurations(videoIds, apiKey, client, ct);

            return data.Items
                .Where(i => !string.IsNullOrEmpty(i.Id.VideoId))
                .Select(i => new YouTubeSearchResult(
                    i.Id.VideoId,
                    i.Snippet.Title,
                    i.Snippet.ChannelTitle ?? "Unknown",
                    durations.GetValueOrDefault(i.Id.VideoId, 0),
                    i.Snippet.Thumbnails?.Medium?.Url
                        ?? i.Snippet.Thumbnails?.Default?.Url
                        ?? $"https://i.ytimg.com/vi/{i.Id.VideoId}/mqdefault.jpg",
                    $"https://www.youtube.com/watch?v={i.Id.VideoId}"))
                .ToList();
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private static async Task<Dictionary<string, int>> GetVideoDurations(
        string videoIds, string apiKey, HttpClient client, CancellationToken ct)
    {
        var result = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(videoIds)) return result;

        try
        {
            var url = $"https://www.googleapis.com/youtube/v3/videos?part=contentDetails&id={videoIds}&key={apiKey}";
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return result;

            var data = await response.Content.ReadFromJsonAsync<YouTubeApiVideoListResponse>(ct);
            if (data?.Items is null) return result;

            foreach (var item in data.Items)
            {
                if (!string.IsNullOrEmpty(item.Id) && !string.IsNullOrEmpty(item.ContentDetails?.Duration))
                {
                    result[item.Id] = ParseIsoDuration(item.ContentDetails.Duration);
                }
            }
        }
        catch
        {
            // duration is best-effort
        }

        return result;
    }

    private static int ParseIsoDuration(string iso)
    {
        // Parse ISO 8601 duration like "PT4M13S", "PT1H2M3S"
        var span = System.Xml.XmlConvert.ToTimeSpan(iso);
        return (int)span.TotalSeconds;
    }

    // ── Piped API fallback ─────────────────────────────────────

    private static readonly string[] PipedInstances =
    [
        "https://pipedapi.tokhmi.xyz",
        "https://pipedapi.kavin.rocks",
        "https://pipedapi.adminforge.de",
    ];

    private static int _pipedIndex;

    private static async Task<List<YouTubeSearchResult>?> SearchViaPiped(
        string query, IHttpClientFactory httpFactory, CancellationToken ct)
    {
        var client = httpFactory.CreateClient("piped");
        client.Timeout = TimeSpan.FromSeconds(10);

        var startIdx = Interlocked.Increment(ref _pipedIndex);

        for (var i = 0; i < PipedInstances.Length; i++)
        {
            var idx = (startIdx + i) % PipedInstances.Length;
            var instance = PipedInstances[idx];

            try
            {
                var url = $"{instance}/search?q={Uri.EscapeDataString(query)}&filter=videos";
                var response = await client.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                    continue;

                // Guard against Cloudflare challenge pages
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (!contentType.Contains("json"))
                    continue;

                var searchResponse = await response.Content.ReadFromJsonAsync<PipedSearchResponse>(ct);
                if (searchResponse?.Items is null)
                    continue;

                return searchResponse.Items
                    .Where(v => !string.IsNullOrEmpty(v.Url) && !string.IsNullOrEmpty(v.Title))
                    .Select(v =>
                    {
                        var videoId = ExtractVideoId(v.Url);
                        return new YouTubeSearchResult(
                            videoId,
                            v.Title,
                            v.UploaderName ?? "Unknown",
                            v.Duration,
                            v.Thumbnail ?? $"https://i.ytimg.com/vi/{videoId}/mqdefault.jpg",
                            $"https://www.youtube.com/watch?v={videoId}");
                    })
                    .Where(r => !string.IsNullOrEmpty(r.VideoId))
                    .ToList();
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // try next instance
            }
        }

        return null;
    }

    private static string ExtractVideoId(string pipedUrl)
    {
        if (string.IsNullOrEmpty(pipedUrl)) return "";
        var idx = pipedUrl.IndexOf("v=", StringComparison.Ordinal);
        if (idx < 0) return "";
        var start = idx + 2;
        var end = pipedUrl.IndexOf('&', start);
        return end < 0 ? pipedUrl[start..] : pipedUrl[start..end];
    }
}

// ── YouTube Data API v3 response models ─────────────────────

internal record YouTubeApiSearchResponse(
    [property: JsonPropertyName("items")] YouTubeApiSearchItem[]? Items);

internal record YouTubeApiSearchItem(
    [property: JsonPropertyName("id")] YouTubeApiVideoId Id,
    [property: JsonPropertyName("snippet")] YouTubeApiSnippet Snippet);

internal record YouTubeApiVideoId(
    [property: JsonPropertyName("videoId")] string VideoId);

internal record YouTubeApiSnippet(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("channelTitle")] string? ChannelTitle,
    [property: JsonPropertyName("thumbnails")] YouTubeApiThumbnails? Thumbnails);

internal record YouTubeApiThumbnails(
    [property: JsonPropertyName("default")] YouTubeApiThumbnail? Default,
    [property: JsonPropertyName("medium")] YouTubeApiThumbnail? Medium);

internal record YouTubeApiThumbnail(
    [property: JsonPropertyName("url")] string Url);

internal record YouTubeApiVideoListResponse(
    [property: JsonPropertyName("items")] YouTubeApiVideoItem[]? Items);

internal record YouTubeApiVideoItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("contentDetails")] YouTubeApiContentDetails? ContentDetails);

internal record YouTubeApiContentDetails(
    [property: JsonPropertyName("duration")] string Duration);

// ── Piped API response models ──────────────────────────────

internal record PipedSearchResponse(
    [property: JsonPropertyName("items")] PipedVideoItem[]? Items);

internal record PipedVideoItem(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("thumbnail")] string? Thumbnail,
    [property: JsonPropertyName("uploaderName")] string? UploaderName,
    [property: JsonPropertyName("duration")] int Duration);

// ── Public response contract ───────────────────────────────

public sealed record YouTubeSearchResult(
    string VideoId,
    string Title,
    string Channel,
    int Duration,
    string ThumbnailUrl,
    string YoutubeUrl);
