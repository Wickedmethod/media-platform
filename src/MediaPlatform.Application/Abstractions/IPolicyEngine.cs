namespace MediaPlatform.Application.Abstractions;

/// <summary>
/// Evaluates playback policies before queue add or playback start.
/// </summary>
public interface IPolicyEngine
{
    PolicyResult Evaluate(PolicyContext context);
    IReadOnlyList<PlaybackPolicy> GetPolicies();
    void AddPolicy(PlaybackPolicy policy);
    void RemovePolicy(string policyId);
    void SetEnabled(string policyId, bool enabled);
}

public record PolicyContext(
    string Action, // "queue-add", "play", "skip", etc.
    string? VideoUrl,
    string? UserId,
    DateTimeOffset Timestamp);

public record PolicyResult(bool Allowed, string? DeniedReason = null, string? DeniedByPolicy = null);

public record PlaybackPolicy(
    string Id,
    string Name,
    PolicyType Type,
    string Value,
    bool Enabled = true);

public enum PolicyType
{
    /// <summary>Block specific YouTube channel IDs.</summary>
    BlockedChannel,

    /// <summary>Only allow playback during certain hours (HH:mm-HH:mm).</summary>
    TimeWindow,

    /// <summary>Maximum queue size.</summary>
    MaxQueueSize,

    /// <summary>Block URLs matching a pattern.</summary>
    BlockedUrlPattern,

    /// <summary>Maximum video duration in seconds.</summary>
    MaxDuration
}
