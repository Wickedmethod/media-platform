using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using Xunit;
using MediaPlatform.Application.Commands;
using MediaPlatform.Domain.Entities;
using NSubstitute;

namespace MediaPlatform.UnitTests;

public class AddToQueueHandlerTests
{
    private readonly IQueueRepository _repository = Substitute.For<IQueueRepository>();
    private readonly IMetadataEnricher _enricher = Substitute.For<IMetadataEnricher>();
    private readonly AddToQueueHandler _sut;

    public AddToQueueHandlerTests()
    {
        _sut = new AddToQueueHandler(_repository, _enricher);
    }

    [Fact]
    public async Task Handle_ValidYouTubeUrl_AddsToQueue()
    {
        var command = new AddToQueueCommand("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "Test Video");

        var item = await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        item.Should().NotBeNull();
        item.Url.Value.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        item.Title.Should().Be("Test Video");
        await _repository.Received(1).AddAsync(Arg.Any<QueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidUrl_Throws()
    {
        var command = new AddToQueueCommand("https://evil.com", "Bad Video");

        var act = () => _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<QueueItem>(), Arg.Any<CancellationToken>());
    }
}
