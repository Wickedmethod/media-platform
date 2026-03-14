using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace MediaPlatform.Api.Authorization;

/// <summary>
/// Transforms Keycloak JWT role claims into standard ASP.NET Core Role claims.
/// Keycloak places realm roles in { "realm_access": { "roles": [...] } }.
/// </summary>
public sealed class KeycloakRoleClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        // Extract from "realm_access" JSON object
        var realmAccess = identity.FindFirst("realm_access");
        if (realmAccess is not null)
        {
            using var doc = JsonDocument.Parse(realmAccess.Value);
            if (doc.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleValue = role.GetString();
                    if (roleValue is not null && !identity.HasClaim(ClaimTypes.Role, roleValue))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                    }
                }
            }
        }

        // Also handle flat "roles" claim (comma-separated)
        var rolesClaim = identity.FindFirst("roles");
        if (rolesClaim is not null)
        {
            foreach (var role in rolesClaim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!identity.HasClaim(ClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        return Task.FromResult(principal);
    }
}
