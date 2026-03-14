using System.Security.Claims;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin").WithTags("Admin");

        // Kill Switch
        group.MapPost("/kill-switch", (KillSwitchRequest request, IKillSwitch killSwitch, IAuditLog auditLog, HttpContext http) =>
        {
            var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            killSwitch.Activate(request.Reason, userId);
            auditLog.Record(new AuditEntry("KILL_SWITCH_ACTIVATED", userId, http.Connection.RemoteIpAddress?.ToString(), request.Reason, DateTimeOffset.UtcNow));
            return Results.Ok(new { status = "active", activatedBy = killSwitch.ActivatedBy, activatedAt = killSwitch.ActivatedAt });
        })
        .WithName("ActivateKillSwitch")
        .WithDescription("Activate the emergency kill switch — blocks all writes");

        group.MapDelete("/kill-switch", (IKillSwitch killSwitch, IAuditLog auditLog, HttpContext http) =>
        {
            var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            killSwitch.Deactivate(userId);
            auditLog.Record(new AuditEntry("KILL_SWITCH_DEACTIVATED", userId, http.Connection.RemoteIpAddress?.ToString(), null, DateTimeOffset.UtcNow));
            return Results.Ok(new { status = "inactive" });
        })
        .WithName("DeactivateKillSwitch")
        .WithDescription("Deactivate the emergency kill switch");

        group.MapGet("/kill-switch", (IKillSwitch killSwitch) =>
        {
            return Results.Ok(new { active = killSwitch.IsActive, activatedBy = killSwitch.ActivatedBy, activatedAt = killSwitch.ActivatedAt });
        })
        .WithName("GetKillSwitchStatus")
        .WithDescription("Get kill switch status");

        // Audit Log
        group.MapGet("/audit", (IAuditLog auditLog, int? count) =>
        {
            var entries = auditLog.GetRecent(count ?? 50);
            return Results.Ok(entries);
        })
        .WithName("GetAuditLog")
        .WithDescription("Get recent audit log entries");

        // Anomaly Detection
        group.MapGet("/anomalies", (IAnomalyDetector detector) =>
        {
            var report = detector.Evaluate();
            return Results.Ok(report);
        })
        .WithName("GetAnomalies")
        .WithDescription("Evaluate current anomaly detection status");
    }
}

public record KillSwitchRequest(string Reason);
