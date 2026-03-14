using FluentAssertions;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.ValueObjects;
using Xunit;

namespace MediaPlatform.UnitTests;

public class QueueItemTests
{
    [Fact]
    public void Constructor_WithStartAtSeconds_StoresValue()
    {
        var item = new QueueItem("id1", VideoUrl.Create("https://youtu.be/abc"), "Test", startAtSeconds: 30);

        item.StartAtSeconds.Should().Be(30);
    }

    [Fact]
    public void Constructor_WithNegativeStartAt_ClampsToZero()
    {
        var item = new QueueItem("id1", VideoUrl.Create("https://youtu.be/abc"), "Test", startAtSeconds: -10);

        item.StartAtSeconds.Should().Be(0);
    }

    [Fact]
    public void Constructor_DefaultStartAt_IsZero()
    {
        var item = new QueueItem("id1", VideoUrl.Create("https://youtu.be/abc"), "Test");

        item.StartAtSeconds.Should().Be(0);
    }
}
