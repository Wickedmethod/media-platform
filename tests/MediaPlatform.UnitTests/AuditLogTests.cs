using FluentAssertions;
using MediaPlatform.Infrastructure.Security;
using Xunit;

namespace MediaPlatform.UnitTests;

public class AuditLogTests
{
    private readonly InMemoryAuditLog _log = new();

    [Fact]
    public void Record_StoresEntry()
    {
        var entry = new Application.Abstractions.AuditEntry("POST /player/play", "user1", "10.0.0.1", "HTTP 200", DateTimeOffset.UtcNow);
        _log.Record(entry);

        var recent = _log.GetRecent(10);
        recent.Should().ContainSingle().Which.Should().Be(entry);
    }

    [Fact]
    public void GetRecent_ReturnsNewestFirst()
    {
        _log.Record(new("action1", null, null, null, DateTimeOffset.UtcNow.AddMinutes(-2)));
        _log.Record(new("action2", null, null, null, DateTimeOffset.UtcNow.AddMinutes(-1)));
        _log.Record(new("action3", null, null, null, DateTimeOffset.UtcNow));

        var recent = _log.GetRecent(10);
        recent.Should().HaveCount(3);
        recent[0].Action.Should().Be("action3");
        recent[2].Action.Should().Be("action1");
    }

    [Fact]
    public void GetRecent_LimitsCount()
    {
        for (var i = 0; i < 10; i++)
            _log.Record(new($"action{i}", null, null, null, DateTimeOffset.UtcNow));

        _log.GetRecent(3).Should().HaveCount(3);
    }

    [Fact]
    public void Record_EvictsOldEntriesWhenFull()
    {
        // Fill to max (1000) + 1
        for (var i = 0; i <= 1000; i++)
            _log.Record(new($"action{i}", null, null, null, DateTimeOffset.UtcNow));

        _log.GetRecent(2000).Should().HaveCountLessThanOrEqualTo(1000);
    }
}
