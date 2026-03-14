using System.Text.RegularExpressions;

namespace MediaPlatform.Application.Validation;

public static partial class QueueItemSanitizer
{
    public static (string Url, string? Title) Sanitize(string url, string? title)
    {
        var sanitizedUrl = url.Trim();
        var sanitizedTitle = title is not null
            ? StripHtml(title.Trim())
            : null;

        return (sanitizedUrl, sanitizedTitle);
    }

    private static string StripHtml(string input) =>
        HtmlTagRegex().Replace(input, string.Empty);

    [GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();
}
