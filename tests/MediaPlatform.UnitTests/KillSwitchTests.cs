using FluentAssertions;
using MediaPlatform.Infrastructure.Security;
using Xunit;

namespace MediaPlatform.UnitTests;

public class KillSwitchTests
{
    private readonly InMemoryKillSwitch _ks = new();

    [Fact]
    public void IsInactive_ByDefault()
    {
        _ks.IsActive.Should().BeFalse();
        _ks.ActivatedBy.Should().BeNull();
        _ks.ActivatedAt.Should().BeNull();
    }

    [Fact]
    public void Activate_SetsActiveState()
    {
        _ks.Activate("suspicious activity", "admin");

        _ks.IsActive.Should().BeTrue();
        _ks.ActivatedBy.Should().Contain("admin").And.Contain("suspicious activity");
        _ks.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ClearsState()
    {
        _ks.Activate("test", "admin");
        _ks.Deactivate("admin");

        _ks.IsActive.Should().BeFalse();
        _ks.ActivatedBy.Should().BeNull();
        _ks.ActivatedAt.Should().BeNull();
    }

    [Fact]
    public void Activate_WithoutUserId_UsesSystem()
    {
        _ks.Activate("auto-detected threat");

        _ks.ActivatedBy.Should().Contain("system");
    }
}
