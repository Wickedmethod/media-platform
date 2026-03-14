using System.Security.Claims;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Application.Queries;
using MediaPlatform.Application.Validation;
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
        })
        .WithName("GetQueue")
        .Produces<IEnumerable<QueueItemResponse>>()
        .WithDescription("List all pending queue items");

        group.MapPost("/add", async (AddToQueueRequest request, AddToQueueHandler handler, IEventBroadcaster events, IPolicyEngine policyEngine, IAuditLog auditLog, HttpContext http, CancellationToken ct) =>
        {
            try
            {
                // Sanitize input
                var (url, title) = QueueItemSanitizer.Sanitize(request.Url, request.Title);

                // Validate
                var validation = QueueItemValidator.Validate(url, title);
                if (!validation.IsValid)
                    return Results.BadRequest(new ApiError(validation.Error!));

                // Evaluate playback policies before adding to queue
                var policyResult = policyEngine.Evaluate(new PolicyContext("queue-add", url, null, DateTimeOffset.UtcNow));
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

                var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = http.User.FindFirst("preferred_username")?.Value
                    ?? http.User.FindFirst(ClaimTypes.Name)?.Value;

                var command = new AddToQueueCommand(url, title ?? string.Empty, request.StartAtSeconds,
                    AddedByUserId: userId, AddedByName: userName);
                var item = await handler.HandleAsync(command, ct);
                events.Broadcast("item-added", new SseEvents.ItemAdded(item.Id, item.Title, item.Url.Value,
                    item.AddedByUserId, item.AddedByName));
                events.Broadcast("queue-updated", new SseEvents.QueueUpdated("add"));
                return Results.Created($"/queue/{item.Id}", MapItem(item));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
        })
        .WithName("AddToQueue")
        .Produces<QueueItemResponse>(StatusCodes.Status201Created)
        .Produces<ApiError>(StatusCodes.Status400BadRequest)
        .Produces<ApiError>(StatusCodes.Status403Forbidden)
        .WithDescription("Add a media item to the queue");

        group.MapDelete("/{id}", async (string id, RemoveFromQueueHandler handler, IQueueRepository repo, IEventBroadcaster events, HttpContext http, CancellationToken ct) =>
        {
            // Ownership check: own item OR media-admin role
            var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = http.User.IsInRole("media-admin");

            if (userId is not null)
            {
                var item = await repo.GetByIdAsync(id, ct);
                if (item is not null && item.AddedByUserId is not null
                    && item.AddedByUserId != userId && !isAdmin)
                {
                    return Results.Json(new ApiError("You can only remove your own items"), statusCode: 403);
                }
            }

            var command = new RemoveFromQueueCommand(id);
            await handler.HandleAsync(command, ct);
            events.Broadcast("queue-updated", new SseEvents.QueueUpdated("remove"));
            return Results.NoContent();
        })
        .WithName("RemoveFromQueue")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiError>(StatusCodes.Status403Forbidden)
        .WithDescription("Remove an item from the queue");

        group.MapGet("/mode", async (IQueueRepository repo, CancellationToken ct) =>
        {
            var mode = await repo.GetQueueModeAsync(ct);
            return Results.Ok(new QueueModeResponse(mode.ToString()));
        })
        .WithName("GetQueueMode")
        .Produces<QueueModeResponse>()
        .WithDescription("Get the current queue mode");

        group.MapPost("/mode", async (SetQueueModeRequest request, SetQueueModeHandler handler, IEventBroadcaster events, CancellationToken ct) =>
        {
            if (!Enum.TryParse<QueueMode>(request.Mode, true, out var mode))
                return Results.BadRequest(new ApiError($"Invalid queue mode: {request.Mode}. Valid modes: Normal, Shuffle, PlayNext"));

            await handler.HandleAsync(new SetQueueModeCommand(mode), ct);
            events.Broadcast("queue-mode", new QueueModeResponse(mode.ToString()));
            return Results.Ok(new QueueModeResponse(mode.ToString()));
        })
        .WithName("SetQueueMode")
        .Produces<QueueModeResponse>()
        .Produces<ApiError>(StatusCodes.Status400BadRequest)
        .WithDescription("Set queue mode (Normal, Shuffle, PlayNext)");
    }

    private static QueueItemResponse MapItem(QueueItem item) =>
        new(item.Id, item.Url.Value, item.Title, item.Status.ToString(), item.AddedAt, item.StartAtSeconds,
            item.AddedByUserId, item.AddedByName);
}
