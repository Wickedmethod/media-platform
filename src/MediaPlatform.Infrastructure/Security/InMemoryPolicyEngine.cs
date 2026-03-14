using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Infrastructure.Security;

public sealed class InMemoryPolicyEngine : IPolicyEngine
{
    private readonly ConcurrentDictionary<string, PlaybackPolicy> _policies = new();

    public PolicyResult Evaluate(PolicyContext context)
    {
        foreach (var policy in _policies.Values.Where(p => p.Enabled))
        {
            var result = EvaluatePolicy(policy, context);
            if (!result.Allowed)
                return result;
        }

        return new PolicyResult(Allowed: true);
    }

    public IReadOnlyList<PlaybackPolicy> GetPolicies() => _policies.Values.ToList();

    public void AddPolicy(PlaybackPolicy policy)
    {
        _policies[policy.Id] = policy;
    }

    public void RemovePolicy(string policyId)
    {
        _policies.TryRemove(policyId, out _);
    }

    public void SetEnabled(string policyId, bool enabled)
    {
        if (_policies.TryGetValue(policyId, out var policy))
        {
            _policies[policyId] = policy with { Enabled = enabled };
        }
    }

    private static PolicyResult EvaluatePolicy(PlaybackPolicy policy, PolicyContext context)
    {
        return policy.Type switch
        {
            PolicyType.BlockedChannel => EvaluateBlockedChannel(policy, context),
            PolicyType.TimeWindow => EvaluateTimeWindow(policy, context),
            PolicyType.BlockedUrlPattern => EvaluateBlockedUrl(policy, context),
            _ => new PolicyResult(Allowed: true)
        };
    }

    private static PolicyResult EvaluateBlockedChannel(PlaybackPolicy policy, PolicyContext context)
    {
        if (context.VideoUrl is null) return new PolicyResult(true);

        // Value contains the blocked channel ID or name
        if (context.VideoUrl.Contains(policy.Value, StringComparison.OrdinalIgnoreCase))
        {
            return new PolicyResult(false, $"Blocked by channel policy: {policy.Name}", policy.Id);
        }

        return new PolicyResult(true);
    }

    private static PolicyResult EvaluateTimeWindow(PlaybackPolicy policy, PolicyContext context)
    {
        // Value format: "HH:mm-HH:mm" (allowed window)
        var parts = policy.Value.Split('-');
        if (parts.Length != 2) return new PolicyResult(true);

        if (TimeOnly.TryParse(parts[0], out var start) && TimeOnly.TryParse(parts[1], out var end))
        {
            var now = TimeOnly.FromDateTime(context.Timestamp.UtcDateTime);

            bool inWindow;
            if (start <= end)
                inWindow = now >= start && now <= end;
            else // Wraps midnight, e.g., "22:00-06:00"
                inWindow = now >= start || now <= end;

            if (!inWindow)
            {
                return new PolicyResult(false, $"Outside allowed time window ({policy.Value})", policy.Id);
            }
        }

        return new PolicyResult(true);
    }

    private static PolicyResult EvaluateBlockedUrl(PlaybackPolicy policy, PolicyContext context)
    {
        if (context.VideoUrl is null) return new PolicyResult(true);

        try
        {
            if (Regex.IsMatch(context.VideoUrl, policy.Value, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
            {
                return new PolicyResult(false, $"URL blocked by pattern: {policy.Name}", policy.Id);
            }
        }
        catch (RegexParseException)
        {
            // Invalid pattern — skip policy rather than crash
        }

        return new PolicyResult(true);
    }
}
