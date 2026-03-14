using FluentAssertions;
using MediaPlatform.Infrastructure.Security;
using Xunit;

namespace MediaPlatform.UnitTests;

public class AnomalyDetectorTests
{
    private readonly SlidingWindowAnomalyDetector _detector = new();

    [Fact]
    public void NoRequests_NoAnomalies()
    {
        var report = _detector.Evaluate();
        report.HasAnomalies.Should().BeFalse();
        report.Alerts.Should().BeEmpty();
    }

    [Fact]
    public void HighDenialRate_TriggersAlert()
    {
        for (var i = 0; i < 25; i++)
            _detector.RecordRequest("/player/play", denied: true, "attacker");

        var report = _detector.Evaluate();
        report.HasAnomalies.Should().BeTrue();
        report.Alerts.Should().Contain(a => a.RuleName == "high-denial-rate" && a.Severity == "critical");
    }

    [Fact]
    public void UserAbuse_TriggersAlert()
    {
        for (var i = 0; i < 12; i++)
            _detector.RecordRequest("/queue/add", denied: true, "bad-user");

        var report = _detector.Evaluate();
        report.HasAnomalies.Should().BeTrue();
        report.Alerts.Should().Contain(a => a.RuleName == "user-abuse");
    }

    [Fact]
    public void RequestSpike_TriggersWarning()
    {
        for (var i = 0; i < 210; i++)
            _detector.RecordRequest("/player/play", denied: false, $"user{i % 50}");

        var report = _detector.Evaluate();
        report.HasAnomalies.Should().BeTrue();
        report.Alerts.Should().Contain(a => a.RuleName == "request-spike" && a.Severity == "warning");
    }

    [Fact]
    public void LowVolume_NormalRequests_NoAlerts()
    {
        for (var i = 0; i < 10; i++)
            _detector.RecordRequest("/queue", denied: false, "user1");

        _detector.RecordRequest("/player/play", denied: true, "user2");

        var report = _detector.Evaluate();
        report.HasAnomalies.Should().BeFalse();
    }
}
