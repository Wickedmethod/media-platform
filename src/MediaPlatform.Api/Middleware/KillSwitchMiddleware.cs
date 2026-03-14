using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Middleware;

/// <summary>
/// Blocks all write (non-GET) API requests when the kill switch is active.
/// Returns 503 Service Unavailable with reason.
/// </summary>
public sealed class KillSwitchMiddleware
{
    private readonly RequestDelegate _next;

    public KillSwitchMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IKillSwitch killSwitch)
    {
        if (killSwitch.IsActive && context.Request.Method != "GET")
        {
            // Allow kill switch deactivation endpoint through
            if (context.Request.Path.StartsWithSegments("/admin/kill-switch") && context.Request.Method == "DELETE")
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Kill switch is active — all write operations are blocked",
                activatedBy = killSwitch.ActivatedBy,
                activatedAt = killSwitch.ActivatedAt
            });
            return;
        }

        await _next(context);
    }
}
