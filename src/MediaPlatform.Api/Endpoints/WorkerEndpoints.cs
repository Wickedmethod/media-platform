using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class WorkerEndpoints
{
    public static void MapWorkerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/worker").WithTags("Worker");

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
    }
}
