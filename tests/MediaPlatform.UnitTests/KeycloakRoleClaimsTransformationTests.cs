using System.Security.Claims;
using FluentAssertions;
using MediaPlatform.Api.Authorization;
using Xunit;

namespace MediaPlatform.UnitTests;

public class KeycloakRoleClaimsTransformationTests
{
    private readonly KeycloakRoleClaimsTransformation _transform = new();

    [Fact]
    public async Task TransformAsync_ExtractsRealmAccessRoles()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("realm_access", """{"roles":["media-admin","media-viewer"]}"""));
        var principal = new ClaimsPrincipal(identity);

        var result = await _transform.TransformAsync(principal);

        var roles = result.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roles.Should().Contain("media-admin");
        roles.Should().Contain("media-viewer");
    }

    [Fact]
    public async Task TransformAsync_HandlesFlatRolesClaim()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("roles", "media-admin, media-operator"));
        var principal = new ClaimsPrincipal(identity);

        var result = await _transform.TransformAsync(principal);

        var roles = result.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roles.Should().Contain("media-admin");
        roles.Should().Contain("media-operator");
    }

    [Fact]
    public async Task TransformAsync_UnauthenticatedPrincipal_NoChange()
    {
        var identity = new ClaimsIdentity(); // not authenticated
        var principal = new ClaimsPrincipal(identity);

        var result = await _transform.TransformAsync(principal);

        result.Claims.Where(c => c.Type == ClaimTypes.Role).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_NoDuplicateRoles()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.Role, "media-admin"));
        identity.AddClaim(new Claim("realm_access", """{"roles":["media-admin"]}"""));
        var principal = new ClaimsPrincipal(identity);

        var result = await _transform.TransformAsync(principal);

        result.Claims.Count(c => c.Type == ClaimTypes.Role && c.Value == "media-admin").Should().Be(1);
    }
}
