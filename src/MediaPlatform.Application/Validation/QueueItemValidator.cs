using System.Text.RegularExpressions;

namespace MediaPlatform.Application.Validation;

public sealed record ValidationResult
{
    public bool IsValid { get; }
    public string? Error { get; }
    public string? VideoId { get; }

    private ValidationResult(bool isValid, string? error, string? videoId)
    {
        IsValid = isValid;
        Error = error;
        VideoId = videoId;
    }

    public static ValidationResult Ok(string videoId) => new(true, null, videoId);
    public static ValidationResult Fail(string error) => new(false, error, null);
}

public static partial class QueueItemValidator
{
    private static readonly Regex[] YouTubePatterns =
    [
        YouTubeWatchRegex(),
        YouTubeShortRegex(),
        YouTubeEmbedRegex(),
        YouTubeMusicRegex(),
    ];

    public static ValidationResult Validate(string? url, string? title)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ValidationResult.Fail("URL is required");

        if (url.Length > 2048)
            return ValidationResult.Fail("URL exceeds maximum length");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "https" && uri.Scheme != "http"))
            return ValidationResult.Fail("Invalid URL format");

        var videoId = ExtractVideoId(url);
        if (videoId is null)
            return ValidationResult.Fail("Only YouTube URLs are supported");

        if (title is { Length: > 200 })
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

    [GeneratedRegex(@"^https?://(?:www\.)?youtube\.com/watch\?.*v=(?<id>[\w-]{11})", RegexOptions.Compiled)]
    private static partial Regex YouTubeWatchRegex();

    [GeneratedRegex(@"^https?://youtu\.be/(?<id>[\w-]{11})", RegexOptions.Compiled)]
    private static partial Regex YouTubeShortRegex();

    [GeneratedRegex(@"^https?://(?:www\.)?youtube\.com/embed/(?<id>[\w-]{11})", RegexOptions.Compiled)]
    private static partial Regex YouTubeEmbedRegex();

    [GeneratedRegex(@"^https?://music\.youtube\.com/watch\?.*v=(?<id>[\w-]{11})", RegexOptions.Compiled)]
    private static partial Regex YouTubeMusicRegex();
}
