using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using Xunit;

namespace MediaPlatform.UnitTests;

public class PlayerLogEntryTests
{
    [Fact]
    public void PlayerLogEntry_ConstructsCorrectly()
    {
        var entry = new PlayerLogEntry(
            "2026-03-14T10:00:00Z", "error", "YouTube player error code 150", "player");

        entry.Timestamp.Should().Be("2026-03-14T10:00:00Z");
        entry.Level.Should().Be("error");
        entry.Message.Should().Be("YouTube player error code 150");
        entry.Source.Should().Be("player");
    }

    [Fact]
    public void PlayerLogEntry_SourceDefaultsToNull()
    {
        var entry = new PlayerLogEntry("2026-03-14T10:00:00Z", "info", "test");
        entry.Source.Should().BeNull();
    }

    [Fact]
    public void PlayerLogPage_HasCorrectShape()
    {
        var entries = new List<PlayerLogEntry>
        {
            new("2026-03-14T10:00:00Z", "info", "Starting up", "tv"),
            new("2026-03-14T10:00:05Z", "error", "Failed to load", "player")
        };
        var page = new PlayerLogPage("living-room-tv", entries, 42);

        page.PlayerId.Should().Be("living-room-tv");
        page.Entries.Should().HaveCount(2);
        page.TotalCount.Should().Be(42);
    }
}
