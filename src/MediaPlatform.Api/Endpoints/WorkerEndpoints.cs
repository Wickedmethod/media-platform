using MediaPlatform.Api.Authorization;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class WorkerEndpoints
{
    public static void MapWorkerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/worker").WithTags("Worker").RequireAuthorization(AuthPolicies.WorkerOnly);

        group.MapPost("/register", async (WorkerRegistrationRequest request, IPlayerRegistry registry, IEventBroadcaster events, CancellationToken ct) =>
        {
            var registration = new WorkerRegistration(
                request.Name,
                request.Capabilities is not null
                    ? new WorkerCapabilities(
                        request.Capabilities.Cec,
                        request.Capabilities.AudioOutput,
                        request.Capabilities.MaxResolution,
                        request.Capabilities.Codecs,
                        request.Capabilities.ChromiumVersion)
                    : null,
                request.Version,
                request.Os);

            var result = await registry.RegisterAsync(registration, ct);

            events.Broadcast("player-online", new SseEvents.PlayerOnline(result.PlayerId, request.Name));

            return Results.Ok(new WorkerRegistrationResponse(
                result.PlayerId,
                result.ServerTime,
                new WorkerConfigResponse(
                    result.Config.HeartbeatInterval,
                    result.Config.PositionReportInterval,
                    result.Config.SseUrl)));
        })
        .WithName("RegisterWorker")
        .Produces<WorkerRegistrationResponse>()
        .WithDescription("Register a player node with the API");

        group.MapPost("/disconnect", async (DisconnectRequest request, IPlayerRegistry registry, IEventBroadcaster events, HttpContext http, CancellationToken ct) =>
        {
            // Resolve playerId from worker key header or require it in request
            // For now, use a header-based approach: X-Player-Id
            var playerId = http.Request.Headers["X-Player-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(playerId))
                return Results.BadRequest(new ApiError("X-Player-Id header is required"));

            await registry.DisconnectAsync(playerId, request.Reason, ct);
            events.Broadcast("player-disconnected", new SseEvents.PlayerDisconnected(playerId, request.Reason));

            return Results.Ok(new { status = "offline", playerId, reason = request.Reason });
        })
        .WithName("DisconnectWorker")
        .WithDescription("Notify API of a planned player disconnect (graceful shutdown)");
    }
}
