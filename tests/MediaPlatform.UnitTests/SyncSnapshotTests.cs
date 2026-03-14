using FluentAssertions;
using MediaPlatform.Api.Endpoints;
using MediaPlatform.Application.Abstractions;
using Xunit;

namespace MediaPlatform.UnitTests;

public class SyncSnapshotTests
{
    [Fact]
    public void SyncSnapshot_ConstructsWithAllFields()
    {
        var queue = new[]
        {
            new QueueItemResponse("1", "https://youtube.com/watch?v=abc", "Test Song", "Pending",
                DateTimeOffset.UtcNow, 0, "user1", "Jonas")
        };

        var nowPlaying = new PlaybackStateResponse("Playing", null, null, 42.5, 0, null);
        var policies = new[] { new PolicySnapshot("p1", "Max Duration", "MaxDuration", true) };

        var snapshot = new SyncSnapshot(
            queue, nowPlaying, "Normal", policies, false, DateTimeOffset.UtcNow, 42);

        snapshot.Queue.Should().HaveCount(1);
        snapshot.NowPlaying.State.Should().Be("Playing");
        snapshot.QueueMode.Should().Be("Normal");
        snapshot.Policies.Should().HaveCount(1);
        snapshot.KillSwitch.Should().BeFalse();
        snapshot.Version.Should().Be(42);
    }

    [Fact]
    public void SyncSnapshot_EmptyState()
    {
        var snapshot = new SyncSnapshot(
            Array.Empty<QueueItemResponse>(),
            new PlaybackStateResponse("Idle", null, null, 0, 0, null),
            "Normal",
            Array.Empty<PolicySnapshot>(),
            false,
            DateTimeOffset.UtcNow,
            0);

        snapshot.Queue.Should().BeEmpty();
        snapshot.NowPlaying.State.Should().Be("Idle");
        snapshot.Version.Should().Be(0);
    }

    [Fact]
    public void SyncSnapshot_KillSwitchActive()
    {
        var snapshot = new SyncSnapshot(
            Array.Empty<QueueItemResponse>(),
            new PlaybackStateResponse("Idle", null, null, 0, 0, null),
            "Normal",
            Array.Empty<PolicySnapshot>(),
            true,
            DateTimeOffset.UtcNow,
            5);

        snapshot.KillSwitch.Should().BeTrue();
    }

    [Fact]
    public void PolicySnapshot_MapsCorrectly()
    {
        var policy = new PolicySnapshot("p1", "Block NSFW", "BlockedChannel", true);

        policy.Id.Should().Be("p1");
        policy.Name.Should().Be("Block NSFW");
        policy.Type.Should().Be("BlockedChannel");
        policy.Enabled.Should().BeTrue();
    }

    [Fact]
    public void HeartbeatRequest_ConstructsCorrectly()
    {
        var req = new HeartbeatRequest("living-room", "Playing", 42.5, "dQw4w9WgXcQ", 3600, "1.0.0");

        req.PlayerId.Should().Be("living-room");
        req.State.Should().Be("Playing");
        req.Position.Should().Be(42.5);
        req.VideoId.Should().Be("dQw4w9WgXcQ");
        req.Uptime.Should().Be(3600);
        req.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void HeartbeatRequest_DefaultValues()
    {
        var req = new HeartbeatRequest("test", "Idle");

        req.Position.Should().Be(0);
        req.VideoId.Should().BeNull();
        req.Uptime.Should().Be(0);
        req.Version.Should().BeNull();
    }

    [Fact]
    public void PlayerStatusResponse_MapsCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var resp = new PlayerStatusResponse("kitchen", now, "Paused", true, 7200, "2.1.0");

        resp.Id.Should().Be("kitchen");
        resp.LastSeen.Should().Be(now);
        resp.State.Should().Be("Paused");
        resp.IsAlive.Should().BeTrue();
        resp.Uptime.Should().Be(7200);
        resp.Version.Should().Be("2.1.0");
    }
}
