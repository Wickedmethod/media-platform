using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace MediaPlatform.UnitTests;

public class ReportErrorHandlerTests
{
    private readonly IQueueRepository _repository = Substitute.For<IQueueRepository>();
    private readonly ReportErrorHandler _sut;

    public ReportErrorHandlerTests()
    {
        _sut = new ReportErrorHandler(_repository);
    }

    [Fact]
    public async Task Handle_FirstError_IncrementsRetryAndSetsError()
    {
        var state = new PlaybackState();
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        state.SetBuffering(item);
        state.SetPlaying(item);
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        var result = await _sut.HandleAsync(new ReportErrorCommand("Network error"), TestContext.Current.CancellationToken);

        result.RetryCount.Should().Be(1);
        result.State.Should().Be(PlayerState.Error);
        result.LastError.Should().Be("Network error");
    }

    [Fact]
    public async Task Handle_ThirdError_SkipsToNextTrack()
    {
        var state = new PlaybackState();
        var item = new QueueItem("q1", VideoUrl.Create("https://youtu.be/abc"), "Test");
        state.SetBuffering(item);
        state.SetPlaying(item);
        // Simulate 2 previous retries
        state.IncrementRetry();
        state.IncrementRetry();
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        var nextItem = new QueueItem("q2", VideoUrl.Create("https://youtu.be/def"), "Next");
        _repository.DequeueNextAsync(Arg.Any<CancellationToken>()).Returns(nextItem);
        _repository.GetQueueModeAsync(Arg.Any<CancellationToken>()).Returns(QueueMode.Normal);

        var result = await _sut.HandleAsync(new ReportErrorCommand("Permanent fail"), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Playing);
        result.CurrentItem!.Id.Should().Be("q2");
    }

    [Fact]
    public async Task Handle_WhenIdleOrStopped_NoOp()
    {
        var state = new PlaybackState();
        _repository.GetPlaybackStateAsync(Arg.Any<CancellationToken>()).Returns(state);

        var result = await _sut.HandleAsync(new ReportErrorCommand("Whatever"), TestContext.Current.CancellationToken);

        result.State.Should().Be(PlayerState.Idle);
        result.RetryCount.Should().Be(0);
    }
}
