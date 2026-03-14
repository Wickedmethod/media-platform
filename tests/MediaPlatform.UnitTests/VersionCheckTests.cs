using FluentAssertions;
using MediaPlatform.Api.Endpoints;
using MediaPlatform.Application.Abstractions;
using Xunit;

namespace MediaPlatform.UnitTests;

public class VersionCheckTests
{
    [Fact]
    public void PlayerVersionInfo_UpToDate_WhenVersionsMatch()
    {
        var info = new PlayerVersionInfo("living-room-tv", "1.3.0", true);
        info.UpToDate.Should().BeTrue();
    }

    [Fact]
    public void PlayerVersionInfo_NotUpToDate_WhenVersionsDiffer()
    {
        var info = new PlayerVersionInfo("bedroom-tv", "1.2.0", false);
        info.UpToDate.Should().BeFalse();
    }

    [Fact]
    public void VersionMatrixResponse_HasExpectedShape()
    {
        var players = new List<PlayerVersionInfo>
        {
            new("living-room-tv", "1.3.0", true),
            new("bedroom-tv", "1.2.0", false)
        };
        var matrix = new VersionMatrixResponse("1.3.0", players);

        matrix.ExpectedVersion.Should().Be("1.3.0");
        matrix.Players.Should().HaveCount(2);
        matrix.Players[0].UpToDate.Should().BeTrue();
        matrix.Players[1].UpToDate.Should().BeFalse();
    }

    [Fact]
    public void VersionMatrixResponse_NullExpectedVersion_AllUpToDate()
    {
        var players = new List<PlayerVersionInfo>
        {
            new("living-room-tv", "1.0.0", true)
        };
        var matrix = new VersionMatrixResponse(null, players);

        matrix.ExpectedVersion.Should().BeNull();
        matrix.Players[0].UpToDate.Should().BeTrue();
    }

    [Fact]
    public void PlayerVersionInfo_NullVersion_IsNotUpToDate()
    {
        var info = new PlayerVersionInfo("test", null, false);
        info.Version.Should().BeNull();
        info.UpToDate.Should().BeFalse();
    }
}

public class DisconnectContractTests
{
    [Fact]
    public void DisconnectRequest_ConstructsCorrectly()
    {
        var request = new DisconnectRequest("shutdown", "SIGTERM");
        request.Reason.Should().Be("shutdown");
        request.Signal.Should().Be("SIGTERM");
    }

    [Fact]
    public void DisconnectRequest_SignalDefaultsToNull()
    {
        var request = new DisconnectRequest("reboot");
        request.Signal.Should().BeNull();
    }
}
