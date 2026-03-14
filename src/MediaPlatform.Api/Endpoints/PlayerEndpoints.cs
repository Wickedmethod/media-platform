using System.Diagnostics;
using MediaPlatform.Application.Abstractions;
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

        group.MapPost("/play", async (PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, events, analytics, notifications, CommandType.Play, ct);
        });

        group.MapPost("/pause", async (PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, events, analytics, notifications, CommandType.Pause, ct);
        });

        group.MapPost("/skip", async (PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, events, analytics, notifications, CommandType.Skip, ct);
        });

        group.MapPost("/stop", async (PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, events, analytics, notifications, CommandType.Stop, ct);
        });

        group.MapPost("/position", async (ReportPositionRequest request, ReportPositionHandler handler, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(new ReportPositionCommand(request.PositionSeconds), ct);
            return Results.Ok(MapState(state));
        });

        group.MapPost("/error", async (ReportErrorRequest request, ReportErrorHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(new ReportErrorCommand(request.Reason), ct);
            analytics.RecordError(request.Reason ?? "unknown");
            events.Broadcast("playback-error", MapState(state));
            _ = notifications.NotifyAsync("playback-error", new { reason = request.Reason, state = state.State.ToString() }, ct);
            return Results.Ok(MapState(state));
        });

        app.MapGet("/now-playing", async (GetPlaybackStateHandler handler, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(ct);
            return Results.Ok(MapState(state));
        }).WithTags("Player");
    }

    private static async Task<IResult> ExecuteCommand(
        PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications,
        CommandType command, CancellationToken ct)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var state = await handler.HandleAsync(new PlayerCommand(command), ct);
            sw.Stop();
            analytics.RecordCommand(command.ToString(), sw.Elapsed.TotalMilliseconds);
            var response = MapState(state);
            events.Broadcast("playback-state", response);
            _ = notifications.NotifyAsync("playback-state", response, ct);
            return Results.Ok(response);
        }
        catch (InvalidStateTransitionException ex)
        {
            return Results.Conflict(new ApiError(ex.Message));
        }
    }

    internal static PlaybackStateResponse MapState(PlaybackState state) =>
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
