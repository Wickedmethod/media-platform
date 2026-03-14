using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace MediaPlatform.UnitTests;

public class ReportPositionHandlerTests
{
    private readonly IQueueRepository _repository = Substitute.For<IQueueRepository>();
    private readonly ReportPositionHandler _sut;

    public ReportPositionHandlerTests()
    {
        _sut = new ReportPositionHandler(_repository);
    }

    [Fact]
    public async Task Handle_UpdatesPositionOnPlaybackState()
    {
        var state = new PlaybackState();
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        state.SetBuffering(item);
        state.SetPlaying(item);
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        var result = await _sut.HandleAsync(new ReportPositionCommand(42.5), TestContext.Current.CancellationToken);

        result.PositionSeconds.Should().Be(42.5);
        await _repository.Received(1).SavePlaybackStateAsync(Arg.Any<PlaybackState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativePosition_DoesNotUpdate()
    {
        var state = new PlaybackState();
        state.UpdatePosition(10);
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        var result = await _sut.HandleAsync(new ReportPositionCommand(-5), TestContext.Current.CancellationToken);

        result.PositionSeconds.Should().Be(10);
    }
}
