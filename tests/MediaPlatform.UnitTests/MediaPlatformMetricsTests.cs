using FluentAssertions;
using MediaPlatform.Infrastructure.Metrics;
using Xunit;

namespace MediaPlatform.UnitTests;

public class MediaPlatformMetricsTests
{
    private readonly MediaPlatformMetrics _metrics = new();

    [Fact]
    public void SetQueueDepth_DoesNotThrow()
    {
        var act = () => _metrics.SetQueueDepth(5);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetPlayerState_DoesNotThrow()
    {
        var act = () => _metrics.SetPlayerState(1);
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementTracksPlayed_DoesNotThrow()
    {
        var act = () => _metrics.IncrementTracksPlayed();
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementPlaybackErrors_DoesNotThrow()
    {
        var act = () => _metrics.IncrementPlaybackErrors();
        act.Should().NotThrow();
    }

    [Fact]
    public void IncrementQueueAdds_DoesNotThrow()
    {
        var act = () => _metrics.IncrementQueueAdds();
        act.Should().NotThrow();
    }

    [Fact]
    public void SetActiveSseConnections_DoesNotThrow()
    {
        var act = () => _metrics.SetActiveSseConnections(3);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetActivePlayers_DoesNotThrow()
    {
        var act = () => _metrics.SetActivePlayers(2);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetKillSwitchActive_DoesNotThrow()
    {
        var act = () => _metrics.SetKillSwitchActive(true);
        act.Should().NotThrow();
    }

    [Fact]
    public void SetKillSwitchActive_False_DoesNotThrow()
    {
        var act = () => _metrics.SetKillSwitchActive(false);
        act.Should().NotThrow();
    }
}
