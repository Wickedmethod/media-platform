using FluentAssertions;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using Xunit;

namespace MediaPlatform.UnitTests;

public class PlaybackStateTests
{
    [Fact]
    public void UpdatePosition_PositiveValue_Updates()
    {
        var state = new PlaybackState();
        state.UpdatePosition(42.5);

        state.PositionSeconds.Should().Be(42.5);
    }

    [Fact]
    public void UpdatePosition_NegativeValue_DoesNotUpdate()
    {
        var state = new PlaybackState();
        state.UpdatePosition(10);
        state.UpdatePosition(-5);

        state.PositionSeconds.Should().Be(10);
    }

    [Fact]
    public void SetIdle_ResetsPositionAndRetry()
    {
        var state = new PlaybackState();
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        state.SetBuffering(item);
        state.SetPlaying(item);
        state.UpdatePosition(100);
        state.IncrementRetry();

        state.SetIdle();

        state.PositionSeconds.Should().Be(0);
        state.RetryCount.Should().Be(0);
        state.LastError.Should().BeNull();
    }

    [Fact]
    public void SetStopped_ResetsPositionAndRetry()
    {
        var state = new PlaybackState();
        state.UpdatePosition(100);
        state.IncrementRetry();

        state.SetStopped();

        state.PositionSeconds.Should().Be(0);
        state.RetryCount.Should().Be(0);
    }

    [Fact]
    public void SetError_RecordsReason()
    {
        var state = new PlaybackState();
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        state.SetBuffering(item);
        state.SetPlaying(item);

        state.SetError("Network timeout");

        state.State.Should().Be(PlayerState.Error);
        state.LastError.Should().Be("Network timeout");
    }

    [Fact]
    public void IncrementRetry_IncrementsCount()
    {
        var state = new PlaybackState();

        state.IncrementRetry();
        state.IncrementRetry();

        state.RetryCount.Should().Be(2);
    }

    [Fact]
    public void ResetRetry_ClearsCountAndError()
    {
        var state = new PlaybackState();
        state.IncrementRetry();
        state.IncrementRetry();
        state.SetError("test");

        state.ResetRetry();

        state.RetryCount.Should().Be(0);
        state.LastError.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParams_SetsValues()
    {
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        var started = DateTimeOffset.UtcNow;

        var state = new PlaybackState(PlayerState.Playing, item, started, 42.5, 2, "some error");

        state.State.Should().Be(PlayerState.Playing);
        state.CurrentItem.Should().Be(item);
        state.StartedAt.Should().Be(started);
        state.PositionSeconds.Should().Be(42.5);
        state.RetryCount.Should().Be(2);
        state.LastError.Should().Be("some error");
    }
}
