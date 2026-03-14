using System.Security.Claims;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Middleware;

/// <summary>
/// Logs all requests with user context to the immutable audit log.
/// Records denied requests to the anomaly detector.
/// </summary>
public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLog auditLog, IAnomalyDetector anomalyDetector)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.FindFirst("sub")?.Value;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var path = context.Request.Path.Value;
        var method = context.Request.Method;

        await _next(context);

        var statusCode = context.Response.StatusCode;
        var denied = statusCode is 401 or 403;

        // Audit security-relevant actions (write operations and denied requests)
        if (method != "GET" || denied)
        {
            auditLog.Record(new AuditEntry(
                Action: $"{method} {path}",
                UserId: userId,
                IpAddress: ip,
                Detail: $"HTTP {statusCode}",
                Timestamp: DateTimeOffset.UtcNow));
        }

        anomalyDetector.RecordRequest(path ?? "/", denied, userId ?? ip);
    }
}
