using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Infrastructure.Security;
using Xunit;

namespace MediaPlatform.UnitTests;

public class PolicyEngineTests
{
    private readonly InMemoryPolicyEngine _engine = new();

    [Fact]
    public void Evaluate_NoPolicies_Allows()
    {
        var result = _engine.Evaluate(new PolicyContext("queue-add", "https://youtube.com/watch?v=abc", null, DateTimeOffset.UtcNow));
        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_BlockedChannel_DeniesMatch()
    {
        _engine.AddPolicy(new PlaybackPolicy("p1", "Block Rick", PolicyType.BlockedChannel, "dQw4w9WgXcQ"));

        var result = _engine.Evaluate(new PolicyContext("queue-add", "https://youtube.com/watch?v=dQw4w9WgXcQ", null, DateTimeOffset.UtcNow));
        result.Allowed.Should().BeFalse();
        result.DeniedByPolicy.Should().Be("p1");
    }

    [Fact]
    public void Evaluate_BlockedChannel_AllowsNonMatch()
    {
        _engine.AddPolicy(new PlaybackPolicy("p1", "Block Rick", PolicyType.BlockedChannel, "dQw4w9WgXcQ"));

        var result = _engine.Evaluate(new PolicyContext("queue-add", "https://youtube.com/watch?v=otherVideo", null, DateTimeOffset.UtcNow));
        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_BlockedUrlPattern_DeniesRegexMatch()
    {
        _engine.AddPolicy(new PlaybackPolicy("p2", "Block shorts", PolicyType.BlockedUrlPattern, @"youtube\.com/shorts/"));

        var result = _engine.Evaluate(new PolicyContext("queue-add", "https://youtube.com/shorts/abc123", null, DateTimeOffset.UtcNow));
        result.Allowed.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_TimeWindow_AllowsWithinWindow()
    {
        var now = DateTimeOffset.UtcNow;
        var start = now.AddHours(-1);
        var end = now.AddHours(1);
        var windowValue = $"{start:HH:mm}-{end:HH:mm}";

        _engine.AddPolicy(new PlaybackPolicy("p3", "Work hours", PolicyType.TimeWindow, windowValue));

        var result = _engine.Evaluate(new PolicyContext("play", null, null, now));
        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_DisabledPolicy_Skipped()
    {
        _engine.AddPolicy(new PlaybackPolicy("p4", "Disabled block", PolicyType.BlockedChannel, "everything", Enabled: false));

        var result = _engine.Evaluate(new PolicyContext("queue-add", "https://youtube.com/watch?v=everything", null, DateTimeOffset.UtcNow));
        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public void AddPolicy_GetPolicies_ReturnsAll()
    {
        _engine.AddPolicy(new PlaybackPolicy("a", "P1", PolicyType.BlockedChannel, "x"));
        _engine.AddPolicy(new PlaybackPolicy("b", "P2", PolicyType.TimeWindow, "08:00-22:00"));

        _engine.GetPolicies().Should().HaveCount(2);
    }

    [Fact]
    public void RemovePolicy_DeletesById()
    {
        _engine.AddPolicy(new PlaybackPolicy("r1", "To Remove", PolicyType.BlockedChannel, "x"));
        _engine.RemovePolicy("r1");

        _engine.GetPolicies().Should().BeEmpty();
    }

    [Fact]
    public void SetEnabled_TogglesPolicy()
    {
        _engine.AddPolicy(new PlaybackPolicy("t1", "Toggle", PolicyType.BlockedChannel, "match", Enabled: true));
        _engine.SetEnabled("t1", false);

        _engine.GetPolicies().Should().ContainSingle().Which.Enabled.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_InvalidRegex_SkipsGracefully()
    {
        _engine.AddPolicy(new PlaybackPolicy("bad", "Bad regex", PolicyType.BlockedUrlPattern, "[invalid"));

        var result = _engine.Evaluate(new PolicyContext("queue-add", "https://example.com", null, DateTimeOffset.UtcNow));
        result.Allowed.Should().BeTrue();
    }
}
