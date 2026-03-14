using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using Xunit;
using MediaPlatform.Application.Commands;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using NSubstitute;

namespace MediaPlatform.UnitTests;

public class PlayerCommandHandlerTests
{
    private readonly IQueueRepository _repository = Substitute.For<IQueueRepository>();
    private readonly PlayerCommandHandler _sut;

    public PlayerCommandHandlerTests()
    {
        _sut = new PlayerCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_Play_WhenIdle_DequeuesAndTransitionsToBuffering()
    {
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(new PlaybackState());
        _repository.DequeueNextAsync(Arg.Any<CancellationToken>()).Returns(item);

        var result = await _sut.HandleAsync(new PlayerCommand(CommandType.Play), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Buffering);
        result.CurrentItem.Should().Be(item);
        await _repository.Received(1).SavePlaybackStateAsync(Arg.Any<PlaybackState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Pause_WhenPlaying_TransitionsToPaused()
    {
        var state = new PlaybackState();
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        state.SetBuffering(item);
        state.SetPlaying(item);
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        var result = await _sut.HandleAsync(new PlayerCommand(CommandType.Pause), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Paused);
    }

    [Fact]
    public async Task Handle_Stop_WhenPlaying_TransitionsToStopped()
    {
        var state = new PlaybackState();
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        state.SetBuffering(item);
        state.SetPlaying(item);
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        var result = await _sut.HandleAsync(new PlayerCommand(CommandType.Stop), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Stopped);
    }

    [Fact]
    public async Task Handle_Skip_WhenPlaying_TransitionsToIdle()
    {
        var state = new PlaybackState();
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        state.SetBuffering(item);
        state.SetPlaying(item);
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        var result = await _sut.HandleAsync(new PlayerCommand(CommandType.Skip), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Idle);
    }
}
