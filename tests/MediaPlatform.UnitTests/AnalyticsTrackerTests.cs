using MediaPlatform.Infrastructure.Analytics;
using Xunit;

namespace MediaPlatform.UnitTests;

public class AnalyticsTrackerTests
{
    [Fact]
    public void RecordCommand_TracksCountAndLatency()
    {
        var tracker = new InMemoryAnalyticsTracker();
        tracker.RecordCommand("Play", 12.5);
        tracker.RecordCommand("Play", 8.3);
        tracker.RecordCommand("Skip", 5.1);

        var snapshot = tracker.GetSnapshot();

        Assert.Equal(3, snapshot.TotalCommands);
        Assert.Equal(2, snapshot.CommandCounts["Play"]);
        Assert.Equal(1, snapshot.CommandCounts["Skip"]);
        Assert.True(snapshot.AverageCommandLatencyMs > 0);
    }

    [Fact]
    public void RecordError_TracksErrors()
    {
        var tracker = new InMemoryAnalyticsTracker();
        tracker.RecordError("Video unavailable");
        tracker.RecordError("Network timeout");

        var snapshot = tracker.GetSnapshot();

        Assert.Equal(2, snapshot.TotalErrors);
        Assert.Equal(2, snapshot.RecentErrors.Count);
        Assert.Equal("Network timeout", snapshot.RecentErrors[0].Reason); // most recent first
    }

    [Fact]
    public void RecordPlaybackTime_AccumulatesSeconds()
    {
        var tracker = new InMemoryAnalyticsTracker();
        tracker.RecordPlaybackTime(30.0);
        tracker.RecordPlaybackTime(45.5);

        var snapshot = tracker.GetSnapshot();

        Assert.Equal(75.5, snapshot.TotalPlaybackSeconds);
    }

    [Fact]
    public void GetSnapshot_EmptyTracker_ReturnsZeros()
    {
        var tracker = new InMemoryAnalyticsTracker();
        var snapshot = tracker.GetSnapshot();

        Assert.Equal(0, snapshot.TotalCommands);
        Assert.Equal(0, snapshot.TotalErrors);
        Assert.Equal(0, snapshot.TotalPlaybackSeconds);
        Assert.Equal(0, snapshot.AverageCommandLatencyMs);
    }
}
