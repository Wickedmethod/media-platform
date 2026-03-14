using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Application.Queries;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;

namespace MediaPlatform.Api.Endpoints;

public static class QueueEndpoints
{
    public static void MapQueueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/queue").WithTags("Queue").RequireRateLimiting("general");

        group.MapGet("/", async (GetQueueHandler handler, CancellationToken ct) =>
        {
            var items = await handler.HandleAsync(ct);
            return Results.Ok(items.Select(MapItem));
        });

        group.MapPost("/add", async (AddToQueueRequest request, AddToQueueHandler handler, IEventBroadcaster events, IPolicyEngine policyEngine, IAuditLog auditLog, HttpContext http, CancellationToken ct) =>
        {
            try
            {
                // Evaluate playback policies before adding to queue
                var policyResult = policyEngine.Evaluate(new PolicyContext("queue-add", request.Url, null, DateTimeOffset.UtcNow));
                if (!policyResult.Allowed)
                {
                    auditLog.Record(new AuditEntry(
                        "POLICY_DENIED",
                        null,
                        http.Connection.RemoteIpAddress?.ToString(),
                        $"queue-add denied: {policyResult.DeniedReason}",
                        DateTimeOffset.UtcNow));
                    return Results.Json(new ApiError(policyResult.DeniedReason ?? "Policy denied", policyResult.DeniedByPolicy), statusCode: 403);
                }

                var command = new AddToQueueCommand(request.Url, request.Title, request.StartAtSeconds);
                var item = await handler.HandleAsync(command, ct);
                events.Broadcast("queue-updated", new { action = "added", item = MapItem(item) });
                return Results.Created($"/queue/{item.Id}", MapItem(item));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
        });

        group.MapDelete("/{id}", async (string id, RemoveFromQueueHandler handler, IEventBroadcaster events, CancellationToken ct) =>
        {
            var command = new RemoveFromQueueCommand(id);
            await handler.HandleAsync(command, ct);
            events.Broadcast("queue-updated", new { action = "removed", itemId = id });
            return Results.NoContent();
        });

        group.MapGet("/mode", async (IQueueRepository repo, CancellationToken ct) =>
        {
            var mode = await repo.GetQueueModeAsync(ct);
            return Results.Ok(new QueueModeResponse(mode.ToString()));
        });

        group.MapPost("/mode", async (SetQueueModeRequest request, SetQueueModeHandler handler, IEventBroadcaster events, CancellationToken ct) =>
        {
            if (!Enum.TryParse<QueueMode>(request.Mode, true, out var mode))
                return Results.BadRequest(new ApiError($"Invalid queue mode: {request.Mode}. Valid modes: Normal, Shuffle, PlayNext"));

            await handler.HandleAsync(new SetQueueModeCommand(mode), ct);
            events.Broadcast("queue-mode", new QueueModeResponse(mode.ToString()));
            return Results.Ok(new QueueModeResponse(mode.ToString()));
        });
    }

    private static QueueItemResponse MapItem(QueueItem item) =>
        new(item.Id, item.Url.Value, item.Title, item.Status.ToString(), item.AddedAt, item.StartAtSeconds);
}
