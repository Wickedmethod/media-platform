using System.Security.Claims;
using System.Text.Encodings.Web;
using MediaPlatform.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MediaPlatform.Api.Authorization;

public sealed class WorkerKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    IAuditLog auditLog)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "WorkerKey";

    private readonly string? _workerKey = configuration.GetValue<string>("Worker:ApiKey");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check header first, then query param (for SSE which can't send headers)
        var key = Request.Headers["X-Worker-Key"].FirstOrDefault()
                  ?? Request.Query["worker-key"].FirstOrDefault();

        if (key is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (string.IsNullOrEmpty(_workerKey))
        {
            auditLog.Record(new AuditEntry(
                "WORKER_AUTH_REJECTED",
                null,
                Context.Connection.RemoteIpAddress?.ToString(),
                "Worker key not configured on server",
                DateTimeOffset.UtcNow));

            return Task.FromResult(AuthenticateResult.Fail("Worker authentication not configured"));
        }

        if (!string.Equals(key, _workerKey, StringComparison.Ordinal))
        {
            auditLog.Record(new AuditEntry(
                "WORKER_AUTH_FAILED",
                null,
                Context.Connection.RemoteIpAddress?.ToString(),
                "Invalid worker API key",
                DateTimeOffset.UtcNow));

            return Task.FromResult(AuthenticateResult.Fail("Invalid worker key"));
        }

        var identity = new ClaimsIdentity(SchemeName);
        identity.AddClaim(new Claim(ClaimTypes.Role, "worker"));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "tv-guest"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "TV"));
        identity.AddClaim(new Claim("origin", "worker"));

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Response.WriteAsJsonAsync(new { error = "Forbidden" });
    }
}
