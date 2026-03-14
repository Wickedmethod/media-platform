using System.Security.Claims;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Middleware;

/// <summary>
/// Authenticates worker requests via X-Worker-Key header.
/// Adds "worker" role claim when the key matches the configured secret.
/// Rejects invalid keys with 403 + audit entry.
/// </summary>
public sealed class WorkerAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _workerKey;

    public WorkerAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _workerKey = configuration.GetValue<string>("Worker:ApiKey");
    }

    public async Task InvokeAsync(HttpContext context, IAuditLog auditLog)
    {
        var headerKey = context.Request.Headers["X-Worker-Key"].FirstOrDefault();

        if (headerKey is not null)
        {
            if (string.IsNullOrEmpty(_workerKey))
            {
                // No worker key configured — reject all worker auth attempts
                auditLog.Record(new AuditEntry(
                    "WORKER_AUTH_REJECTED",
                    null,
                    context.Connection.RemoteIpAddress?.ToString(),
                    "Worker key not configured on server",
                    DateTimeOffset.UtcNow));

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Worker authentication not configured" });
                return;
            }

            if (!string.Equals(headerKey, _workerKey, StringComparison.Ordinal))
            {
                auditLog.Record(new AuditEntry(
                    "WORKER_AUTH_FAILED",
                    null,
                    context.Connection.RemoteIpAddress?.ToString(),
                    "Invalid worker API key",
                    DateTimeOffset.UtcNow));

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid worker key" });
                return;
            }

            // Valid worker key — add worker identity
            var identity = new ClaimsIdentity("WorkerKey");
            identity.AddClaim(new Claim(ClaimTypes.Role, "worker"));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "worker-node"));
            identity.AddClaim(new Claim("origin", "worker"));
            context.User.AddIdentity(identity);
        }

        await _next(context);
    }
}
