namespace MediaPlatform.Domain.ValueObjects;

public sealed record VideoUrl
{
    private static readonly HashSet<string> AllowedHosts =
    [
        "www.youtube.com",
        "youtube.com",
        "youtu.be",
        "m.youtube.com"
    ];

    public string Value { get; }

    private VideoUrl(string value) => Value = value;

    public static VideoUrl Create(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid URL format.", nameof(url));

        if (!AllowedHosts.Contains(uri.Host))
            throw new ArgumentException("Only YouTube URLs are allowed.", nameof(url));

        return new VideoUrl(uri.ToString());
    }

    public override string ToString() => Value;
}
