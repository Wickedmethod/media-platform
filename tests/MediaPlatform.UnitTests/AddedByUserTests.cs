using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace MediaPlatform.UnitTests;

public class AddedByUserTests
{
    private readonly IQueueRepository _repository = Substitute.For<IQueueRepository>();
    private readonly IMetadataEnricher _enricher = Substitute.For<IMetadataEnricher>();

    [Fact]
    public async Task Handle_WithUserFields_StoresUserInfo()
    {
        var handler = new AddToQueueHandler(_repository, _enricher);
        var command = new AddToQueueCommand(
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ", "Test",
            AddedByUserId: "user-123", AddedByName: "jonas");

        var item = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        item.AddedByUserId.Should().Be("user-123");
        item.AddedByName.Should().Be("jonas");
    }

    [Fact]
    public async Task Handle_WithoutUserFields_StoresNull()
    {
        var handler = new AddToQueueHandler(_repository, _enricher);
        var command = new AddToQueueCommand("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "Test");

        var item = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        item.AddedByUserId.Should().BeNull();
        item.AddedByName.Should().BeNull();
    }

    [Fact]
    public void QueueItem_NewWithUser_HasUserFields()
    {
        var url = VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        var item = new QueueItem("id1", url, "Title", 0, "user-abc", "alice");

        item.AddedByUserId.Should().Be("user-abc");
        item.AddedByName.Should().Be("alice");
    }

    [Fact]
    public void QueueItem_NewWithoutUser_HasNullUserFields()
    {
        var url = VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        var item = new QueueItem("id1", url, "Title");

        item.AddedByUserId.Should().BeNull();
        item.AddedByName.Should().BeNull();
    }

    [Fact]
    public void QueueItem_DeserializationConstructor_PreservesUserFields()
    {
        var url = VideoUrl.Create("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        var addedAt = DateTimeOffset.UtcNow;
        var item = new QueueItem("id1", url, "Title", Domain.Enums.QueueItemStatus.Pending,
            addedAt, 0, "user-xyz", "bob");

        item.AddedByUserId.Should().Be("user-xyz");
        item.AddedByName.Should().Be("bob");
    }
}
