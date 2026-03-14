using System.Security.Claims;
using MediaPlatform.Api.Authorization;
using MediaPlatform.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace MediaPlatform.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin").WithTags("Admin").RequireAuthorization(AuthPolicies.AdminOnly);

        // Player Liveness
        group.MapGet("/players", async (IPlayerRegistry registry, CancellationToken ct) =>
        {
            var players = await registry.GetAllPlayersAsync(ct);
            return Results.Ok(players.Select(p => new PlayerStatusResponse(
                p.Id, p.LastSeen, p.State, p.IsAlive, p.Uptime, p.Version,
                p.Name, p.Capabilities, p.RegisteredAt)));
        })
        .WithName("GetPlayers")
        .Produces<IEnumerable<PlayerStatusResponse>>()
        .WithDescription("List all registered players with liveness status");

        // Player Logs (MEDIA-732)
        group.MapGet("/players/{id}/logs", async (string id, IPlayerLogStore logStore, string? level, int? limit, CancellationToken ct) =>
        {
            var page = await logStore.GetLogsAsync(id, level, limit ?? 100, ct);
            return Results.Ok(new PlayerLogResponse(
                page.PlayerId,
                page.Entries.Select(e => new LogEntryResponse(e.Timestamp, e.Level, e.Message, e.Source)).ToList(),
                page.TotalCount));
        })
        .WithName("GetPlayerLogs")
        .Produces<PlayerLogResponse>()
        .WithDescription("Get diagnostic logs for a specific player");

        // Version Matrix (MEDIA-733)
        group.MapGet("/players/versions", async (IPlayerRegistry registry, IConfiguration config, CancellationToken ct) =>
        {
            var expectedVersion = config.GetValue<string>("Player:ExpectedVersion");
            var players = await registry.GetAllPlayersAsync(ct);
            var versions = players.Select(p => new PlayerVersionInfo(
                p.Id,
                p.Version,
                expectedVersion is null || p.Version == expectedVersion)).ToList();
            return Results.Ok(new VersionMatrixResponse(expectedVersion, versions));
        })
        .WithName("GetVersionMatrix")
        .Produces<VersionMatrixResponse>()
        .WithDescription("List all players with version comparison against expected version");

        // Set Expected Version (MEDIA-733)
        group.MapPost("/players/expected-version", (SetExpectedVersionRequest request, IConfiguration config) =>
        {
            // Store in-memory via config override (for runtime use)
            config["Player:ExpectedVersion"] = request.Version;
            return Results.Ok(new { expectedVersion = request.Version });
        })
        .WithName("SetExpectedVersion")
        .WithDescription("Set the expected player software version");

        // Broadcast Update Notice (MEDIA-733)
        group.MapPost("/players/notify-update", (NotifyUpdateRequest request, IEventBroadcaster events, IConfiguration config) =>
        {
            var version = config.GetValue<string>("Player:ExpectedVersion") ?? "unknown";
            events.Broadcast("update-available", new SseEvents.UpdateAvailable(version, request.Message));
            return Results.Ok(new { notified = true, version, message = request.Message });
        })
        .WithName("NotifyPlayerUpdate")
        .WithDescription("Broadcast an update-available SSE event to all connected players");

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

        // MEDIA-763: Player Network Metrics
        group.MapGet("/players/{id}/network", async (string id, INetworkMetricsStore networkStore, CancellationToken ct) =>
        {
            var current = await networkStore.GetCurrentAsync(id, ct);
            var trend = await networkStore.GetTrendAsync(id, ct);

            NetworkMetricsCurrentDto? currentDto = current is null ? null : new(
                current.PlayerId, current.Timestamp,
                new LatencyMetricsDto(current.Latency.AvgMs, current.Latency.MinMs, current.Latency.MaxMs,
                    current.Latency.P95Ms, current.Latency.Samples, current.Latency.Failures),
                new DnsMetricsDto(current.Dns.AvgResolveMs, current.Dns.Failures),
                new BandwidthMetricsDto(current.Bandwidth.LastMbps, current.Bandwidth.MeasuredAt));

            var trendDto = new NetworkTrendDto(trend.LatencyTrend, trend.AvgLatency1h, trend.BandwidthTrend, trend.AvgBandwidth1h);
            return Results.Ok(new NetworkMetricsResponse(currentDto, trendDto));
        })
        .WithName("GetPlayerNetworkMetrics")
        .Produces<NetworkMetricsResponse>()
        .WithDescription("Get network connectivity metrics and trend for a specific player");

        // MEDIA-743: Alert configuration status
        group.MapGet("/alerts/config", (Microsoft.Extensions.Options.IOptions<AlertingOptions> options) =>
        {
            var cfg = options.Value;
            return Results.Ok(new AlertConfigResponse(cfg.Enabled, cfg.CooldownMinutes, cfg.Channels.Count));
        })
        .WithName("GetAlertConfig")
        .Produces<AlertConfigResponse>()
        .WithDescription("Get current alerting configuration status");
    }
}

public record KillSwitchRequest(string Reason);
