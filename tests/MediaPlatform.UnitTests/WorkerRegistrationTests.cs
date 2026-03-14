using FluentAssertions;
using MediaPlatform.Application.Abstractions;
using Xunit;

namespace MediaPlatform.UnitTests;

public class WorkerRegistrationTests
{
    [Fact]
    public void WorkerRegistration_ConstructsCorrectly()
    {
        var caps = new WorkerCapabilities(Cec: true, AudioOutput: "hdmi", MaxResolution: "4k");
        var reg = new WorkerRegistration("Living Room TV", caps, "1.2.0", "linux");

        reg.Name.Should().Be("Living Room TV");
        reg.Capabilities.Should().NotBeNull();
        reg.Capabilities!.Cec.Should().BeTrue();
        reg.Capabilities.AudioOutput.Should().Be("hdmi");
        reg.Capabilities.MaxResolution.Should().Be("4k");
        reg.Version.Should().Be("1.2.0");
        reg.Os.Should().Be("linux");
    }

    [Fact]
    public void WorkerRegistration_NullCapabilities_IsValid()
    {
        var reg = new WorkerRegistration("Bedroom", null, null, null);

        reg.Name.Should().Be("Bedroom");
        reg.Capabilities.Should().BeNull();
        reg.Version.Should().BeNull();
        reg.Os.Should().BeNull();
    }

    [Fact]
    public void WorkerCapabilities_Defaults_AreFalseAndNull()
    {
        var caps = new WorkerCapabilities();

        caps.Cec.Should().BeFalse();
        caps.AudioOutput.Should().BeNull();
        caps.MaxResolution.Should().BeNull();
        caps.Codecs.Should().BeNull();
        caps.ChromiumVersion.Should().BeNull();
    }

    [Fact]
    public void WorkerCapabilities_WithCodecs_StoresAll()
    {
        var codecs = new List<string> { "h264", "vp9", "av1" };
        var caps = new WorkerCapabilities(Codecs: codecs);

        caps.Codecs.Should().HaveCount(3);
        caps.Codecs.Should().Contain("av1");
    }

    [Fact]
    public void WorkerRegistrationResult_HasPlayerId()
    {
        var config = new WorkerConfig();
        var result = new WorkerRegistrationResult("living-room-tv", DateTimeOffset.UtcNow, config);

        result.PlayerId.Should().Be("living-room-tv");
        result.Config.HeartbeatInterval.Should().Be(30);
        result.Config.PositionReportInterval.Should().Be(5);
        result.Config.SseUrl.Should().Be("/events");
    }

    [Fact]
    public void WorkerConfig_HasDefaults()
    {
        var config = new WorkerConfig();

        config.HeartbeatInterval.Should().Be(30);
        config.PositionReportInterval.Should().Be(5);
        config.SseUrl.Should().Be("/events");
    }

    [Fact]
    public void PlayerStatus_WithRegistrationFields_ConstructsCorrectly()
    {
        var caps = new WorkerCapabilities(Cec: true, AudioOutput: "hdmi");
        var status = new PlayerStatus(
            "living-room-tv",
            DateTimeOffset.UtcNow,
            "Playing",
            IsAlive: true,
            Uptime: 3600,
            Version: "1.0.0",
            Name: "Living Room TV",
            Capabilities: caps,
            RegisteredAt: DateTimeOffset.UtcNow.AddHours(-1));

        status.Name.Should().Be("Living Room TV");
        status.Capabilities.Should().NotBeNull();
        status.Capabilities!.Cec.Should().BeTrue();
        status.RegisteredAt.Should().NotBeNull();
    }

    [Fact]
    public void PlayerStatus_WithoutRegistrationFields_DefaultsToNull()
    {
        var status = new PlayerStatus(
            "test",
            DateTimeOffset.UtcNow,
            "Idle",
            IsAlive: false,
            Uptime: 0,
            Version: null);

        status.Name.Should().BeNull();
        status.Capabilities.Should().BeNull();
        status.RegisteredAt.Should().BeNull();
    }
}
