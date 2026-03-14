using MediaPlatform.Application.Commands;
using MediaPlatform.Application.Queries;
using MediaPlatform.Domain.Entities;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.Errors;

namespace MediaPlatform.Api.Endpoints;

public static class PlayerEndpoints
{
    public static void MapPlayerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/player").WithTags("Player");

        group.MapPost("/play", async (PlayerCommandHandler handler, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, CommandType.Play, ct);
        });

        group.MapPost("/pause", async (PlayerCommandHandler handler, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, CommandType.Pause, ct);
        });

        group.MapPost("/skip", async (PlayerCommandHandler handler, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, CommandType.Skip, ct);
        });

        group.MapPost("/stop", async (PlayerCommandHandler handler, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, CommandType.Stop, ct);
        });

        group.MapPost("/position", async (ReportPositionRequest request, ReportPositionHandler handler, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(new ReportPositionCommand(request.PositionSeconds), ct);
            return Results.Ok(MapState(state));
        });

        group.MapPost("/error", async (ReportErrorRequest request, ReportErrorHandler handler, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(new ReportErrorCommand(request.Reason), ct);
            return Results.Ok(MapState(state));
        });

        app.MapGet("/now-playing", async (GetPlaybackStateHandler handler, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(ct);
            return Results.Ok(MapState(state));
        }).WithTags("Player");
    }

    private static async Task<IResult> ExecuteCommand(
        PlayerCommandHandler handler, CommandType command, CancellationToken ct)
    {
        try
        {
            var state = await handler.HandleAsync(new PlayerCommand(command), ct);
            return Results.Ok(MapState(state));
        }
        catch (InvalidStateTransitionException ex)
        {
            return Results.Conflict(new ApiError(ex.Message));
        }
    }

    private static PlaybackStateResponse MapState(PlaybackState state) =>
        new(
            state.State.ToString(),
            state.CurrentItem is not null
                ? new QueueItemResponse(
                    state.CurrentItem.Id,
                    state.CurrentItem.Url.Value,
                    state.CurrentItem.Title,
                    state.CurrentItem.Status.ToString(),
                    state.CurrentItem.AddedAt,
                    state.CurrentItem.StartAtSeconds)
                : null,
            state.StartedAt,
            state.PositionSeconds,
            state.RetryCount,
            state.LastError);
}
