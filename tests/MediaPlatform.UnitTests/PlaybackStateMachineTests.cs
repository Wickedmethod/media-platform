using FluentAssertions;
using MediaPlatform.Domain;
using Xunit;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.Errors;
using MediaPlatform.Domain.ValueObjects;

namespace MediaPlatform.UnitTests;

public class PlaybackStateMachineTests
{
    private static QueueItem CreateItem(string id = "test-1") =>
        new(id, VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ"), "Test Video");

    [Fact]
    public void Play_FromIdle_TransitionsToBuffering()
    {
        var state = new PlaybackState();
        var item = CreateItem();

        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);

        state.State.Should().Be(PlayerState.Buffering);
        state.CurrentItem.Should().Be(item);
    }

    [Fact]
    public void Play_FromPaused_TransitionsToPlaying()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);
        PlaybackStateMachine.Apply(state, PlayerEvent.PauseRequested);

        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested);

        state.State.Should().Be(PlayerState.Playing);
    }

    [Fact]
    public void Play_WhenAlreadyPlaying_IsIdempotent()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested);

        state.State.Should().Be(PlayerState.Playing);
    }

    [Fact]
    public void Pause_FromPlaying_TransitionsToPaused()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        PlaybackStateMachine.Apply(state, PlayerEvent.PauseRequested);

        state.State.Should().Be(PlayerState.Paused);
    }

    [Fact]
    public void Pause_WhenAlreadyPaused_IsIdempotent()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);
        PlaybackStateMachine.Apply(state, PlayerEvent.PauseRequested);

        PlaybackStateMachine.Apply(state, PlayerEvent.PauseRequested);

        state.State.Should().Be(PlayerState.Paused);
    }

    [Fact]
    public void Skip_FromPlaying_TransitionsToIdle()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        PlaybackStateMachine.Apply(state, PlayerEvent.SkipRequested);

        state.State.Should().Be(PlayerState.Idle);
        state.CurrentItem.Should().BeNull();
    }

    [Fact]
    public void Skip_WhenIdle_IsIdempotent()
    {
        var state = new PlaybackState();

        PlaybackStateMachine.Apply(state, PlayerEvent.SkipRequested);

        state.State.Should().Be(PlayerState.Idle);
    }

    [Fact]
    public void Stop_FromPlaying_TransitionsToStopped()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        PlaybackStateMachine.Apply(state, PlayerEvent.StopRequested);

        state.State.Should().Be(PlayerState.Stopped);
    }

    [Fact]
    public void Stop_WhenAlreadyStopped_IsIdempotent()
    {
        var state = new PlaybackState();
        PlaybackStateMachine.Apply(state, PlayerEvent.StopRequested);

        PlaybackStateMachine.Apply(state, PlayerEvent.StopRequested);

        state.State.Should().Be(PlayerState.Stopped);
    }

    [Fact]
    public void TrackEnded_FromPlaying_TransitionsToIdle()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        PlaybackStateMachine.Apply(state, PlayerEvent.TrackEnded);

        state.State.Should().Be(PlayerState.Idle);
    }

    [Fact]
    public void Error_SetsErrorState()
    {
        var state = new PlaybackState();
        var item = CreateItem();
        PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested, item);
        PlaybackStateMachine.Apply(state, PlayerEvent.PlaybackStarted);

        PlaybackStateMachine.Apply(state, PlayerEvent.ErrorOccurred);

        state.State.Should().Be(PlayerState.Error);
    }

    [Fact]
    public void Play_FromIdle_WithoutItem_Throws()
    {
        var state = new PlaybackState();

        var act = () => PlaybackStateMachine.Apply(state, PlayerEvent.PlayRequested);

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Pause_FromIdle_Throws()
    {
        var state = new PlaybackState();

        var act = () => PlaybackStateMachine.Apply(state, PlayerEvent.PauseRequested);

        act.Should().Throw<InvalidStateTransitionException>();
    }
}
