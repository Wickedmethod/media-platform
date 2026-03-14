using FluentAssertions;
using MediaPlatform.Domain;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using Xunit;

namespace MediaPlatform.UnitTests;

public class StateMachineErrorReasonTests
{
    private static QueueItem CreateItem() =>
        new("test-1", VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ"), "Test");

    [Fact]
    public void ErrorOccurred_WithReason_RecordsReason()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        PlaybackStateMachine.Apply(state, PlayerEvent.ErrorOccurred, errorReason: "Network timeout");

        state.State.Should().Be(PlayerState.Error);
        state.LastError.Should().Be("Network timeout");
    }

    [Fact]
    public void ErrorOccurred_WithoutReason_ErrorIsNull()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        PlaybackStateMachine.Apply(state, PlayerEvent.ErrorOccurred);

        state.State.Should().Be(PlayerState.Error);
        state.LastError.Should().BeNull();
    }
}
