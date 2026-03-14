using MediaPlatform.Application.Commands;
using MediaPlatform.Application.Queries;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.Errors;

namespace MediaPlatform.Api.Endpoints;

public static class QueueEndpoints
{
    public static void MapQueueEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/queue").WithTags("Queue");

        group.MapGet("/", async (GetQueueHandler handler, CancellationToken ct) =>
        {
            var items = await handler.HandleAsync(ct);
            return Results.Ok(items.Select(MapItem));
        });

        group.MapPost("/add", async (AddToQueueRequest request, AddToQueueHandler handler, CancellationToken ct) =>
        {
            try
            {
                var command = new AddToQueueCommand(request.Url, request.Title);
                var item = await handler.HandleAsync(command, ct);
                return Results.Created($"/queue/{item.Id}", MapItem(item));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapDelete("/{id}", async (string id, RemoveFromQueueHandler handler, CancellationToken ct) =>
        {
            var command = new RemoveFromQueueCommand(id);
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        });
    }

    private static QueueItemResponse MapItem(QueueItem item) =>
        new(item.Id, item.Url.Value, item.Title, item.Status.ToString(), item.AddedAt);
}
