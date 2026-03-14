using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Infrastructure.Redis;
using NSubstitute;
using StackExchange.Redis;
using Xunit;

namespace MediaPlatform.UnitTests;

public class PlayerRegistryTests
{
    [Fact]
    public void PlayerStatus_IsAlive_TrueWhenLastSeenRecently()
    {
        var status = new PlayerStatus(
            "living-room",
            DateTimeOffset.UtcNow.AddSeconds(-30),
            "Playing",
            IsAlive: true,
            Uptime: 3600,
            Version: "1.0.0");

        status.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void PlayerStatus_IsAlive_FalseWhenStale()
    {
        var status = new PlayerStatus(
            "bedroom",
            DateTimeOffset.UtcNow.AddSeconds(-100),
            "Idle",
            IsAlive: false,
            Uptime: 500,
            Version: "1.0.0");

        status.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void PlayerHeartbeat_ConstructsCorrectly()
    {
        var heartbeat = new PlayerHeartbeat(
            "kitchen", "Playing", 42.5, "dQw4w9WgXcQ", 1200, "2.0.0");

        heartbeat.PlayerId.Should().Be("kitchen");
        heartbeat.State.Should().Be("Playing");
        heartbeat.Position.Should().Be(42.5);
        heartbeat.VideoId.Should().Be("dQw4w9WgXcQ");
        heartbeat.Uptime.Should().Be(1200);
        heartbeat.Version.Should().Be("2.0.0");
    }

    [Fact]
    public void PlayerHeartbeat_NullableFields_DefaultCorrectly()
    {
        var heartbeat = new PlayerHeartbeat("test", "Idle", 0, null, 0, null);

        heartbeat.VideoId.Should().BeNull();
        heartbeat.Version.Should().BeNull();
    }
}
