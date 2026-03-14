using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MediaPlatform.Api.Authorization;

/// <summary>
/// Auto-authenticates all requests as admin in development mode (no Keycloak).
/// Registered for both "Bearer" and "WorkerKey" schemes so all policies pass.
/// </summary>
public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity("Development");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "dev-user"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "Developer"));
        identity.AddClaim(new Claim(ClaimTypes.Role, MediaPlatformRoles.Admin));
        identity.AddClaim(new Claim(ClaimTypes.Role, MediaPlatformRoles.Operator));
        identity.AddClaim(new Claim(ClaimTypes.Role, MediaPlatformRoles.Viewer));
        identity.AddClaim(new Claim(ClaimTypes.Role, "worker"));

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
