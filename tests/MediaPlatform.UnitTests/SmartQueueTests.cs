using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace MediaPlatform.UnitTests;

public class SmartQueueTests
{
    private readonly IQueueRepository _repository = Substitute.For<IQueueRepository>();
    private readonly PlayerCommandHandler _sut;

    public SmartQueueTests()
    {
        _sut = new PlayerCommandHandler(_repository);
    }

    [Fact]
    public async Task Play_ShuffleMode_UsesDequeueShuffled()
    {
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(new PlaybackState());
        _repository.GetQueueModeAsync(Arg.Any<CancellationToken>()).Returns(QueueMode.Shuffle);
        _repository.DequeueShuffledAsync(Arg.Any<CancellationToken>()).Returns(item);

        var result = await _sut.HandleAsync(new PlayerCommand(CommandType.Play), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Playing);
        await _repository.Received(1).DequeueShuffledAsync(Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().DequeueNextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Play_NormalMode_UsesDequeueNext()
    {
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(new PlaybackState());
        _repository.GetQueueModeAsync(Arg.Any<CancellationToken>()).Returns(QueueMode.Normal);
        _repository.DequeueNextAsync(Arg.Any<CancellationToken>()).Returns(item);

        var result = await _sut.HandleAsync(new PlayerCommand(CommandType.Play), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Playing);
        await _repository.Received(1).DequeueNextAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Skip_AutoAdvancesToNextTrack()
    {
        var currentItem = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Current");
        var state = new PlaybackState();
        state.SetBuffering(currentItem);
        state.SetPlaying(currentItem);
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);
        _repository.GetQueueModeAsync(Arg.Any<CancellationToken>()).Returns(QueueMode.Normal);

        var nextItem = new QueueItem("q2", VideoUrl.Create("https://youtu.be/def"), "Next");
        _repository.DequeueNextAsync(Arg.Any<CancellationToken>()).Returns(nextItem);

        var result = await _sut.HandleAsync(new PlayerCommand(CommandType.Skip), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Playing);
        result.CurrentItem!.Id.Should().Be("q2");
    }

    [Fact]
    public async Task Skip_NoMoreItems_TransitionsToIdle()
    {
        var currentItem = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Current");
        var state = new PlaybackState();
        state.SetBuffering(currentItem);
        state.SetPlaying(currentItem);
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);
        _repository.GetQueueModeAsync(Arg.Any<CancellationToken>()).Returns(QueueMode.Normal);
        _repository.DequeueNextAsync(Arg.Any<CancellationToken>()).Returns((QueueItem?)null);

        var result = await _sut.HandleAsync(new PlayerCommand(CommandType.Skip), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Idle);
    }

    [Fact]
    public async Task SetQueueMode_PersistsMode()
    {
        var handler = new SetQueueModeHandler(_repository);

        var result = await handler.HandleAsync(new SetQueueModeCommand(QueueMode.Shuffle), TestContext.Current.CancellationToken);

        result.Should().Be(QueueMode.Shuffle);
        await _repository.Received(1).SetQueueModeAsync(QueueMode.Shuffle, Arg.Any<CancellationToken>());
    }
}
