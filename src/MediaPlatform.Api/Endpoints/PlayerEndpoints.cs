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
        var group = app.MapGroup("/player").WithTags("Player").RequireRateLimiting("commands");

        group.MapPost("/heartbeat", async (HeartbeatRequest request, IPlayerRegistry registry, IEventBroadcaster events, CancellationToken ct) =>
        {
            // Check for zombies before recording new heartbeat
            var playersBefore = await registry.GetAllPlayersAsync(ct);
            var wasAlive = playersBefore.Any(p => p.Id == request.PlayerId && p.IsAlive);

            await registry.RecordHeartbeatAsync(
                new PlayerHeartbeat(request.PlayerId, request.State, request.Position,
                    request.VideoId, request.Uptime, request.Version), ct);

            // Detect any newly-offline players
            var playersAfter = await registry.GetAllPlayersAsync(ct);
            foreach (var player in playersAfter.Where(p => !p.IsAlive))
            {
                // Only emit for players that were previously alive (not for the current heartbeat sender going offline)
                var wasPreviouslyAlive = playersBefore.Any(p => p.Id == player.Id && p.IsAlive);
                if (wasPreviouslyAlive)
                {
                    events.Broadcast("player-offline", new SseEvents.PlayerOffline(player.Id));
                }
            }

            return Results.NoContent();
        })
        .WithName("PlayerHeartbeat")
        .Produces(StatusCodes.Status204NoContent)
        .WithDescription("Report player liveness heartbeat");

        group.MapPost("/play", async (PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, IQueueRepository repo, CancellationToken ct) =>
        {
            var result = await ExecuteCommand(handler, events, analytics, notifications, CommandType.Play, ct);
            await repo.IncrementVersionAsync(ct);
            return result;
        })
        .WithName("Play")
        .Produces<PlaybackStateResponse>()
        .Produces<ApiError>(StatusCodes.Status409Conflict)
        .WithDescription("Start or resume playback");

        group.MapPost("/pause", async (PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, events, analytics, notifications, CommandType.Pause, ct);
        })
        .WithName("Pause")
        .Produces<PlaybackStateResponse>()
        .Produces<ApiError>(StatusCodes.Status409Conflict)
        .WithDescription("Pause playback");

        group.MapPost("/skip", async (PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, IQueueRepository repo, CancellationToken ct) =>
        {
            var result = await ExecuteCommand(handler, events, analytics, notifications, CommandType.Skip, ct);
            await repo.IncrementVersionAsync(ct);
            return result;
        })
        .WithName("Skip")
        .Produces<PlaybackStateResponse>()
        .Produces<ApiError>(StatusCodes.Status409Conflict)
        .WithDescription("Skip to the next track");

        group.MapPost("/stop", async (PlayerCommandHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, CancellationToken ct) =>
        {
            return await ExecuteCommand(handler, events, analytics, notifications, CommandType.Stop, ct);
        })
        .WithName("Stop")
        .Produces<PlaybackStateResponse>()
        .Produces<ApiError>(StatusCodes.Status409Conflict)
        .WithDescription("Stop playback");

        group.MapPost("/position", async (ReportPositionRequest request, ReportPositionHandler handler, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(new ReportPositionCommand(request.PositionSeconds), ct);
            return Results.Ok(MapState(state));
        })
        .WithName("ReportPosition")
        .Produces<PlaybackStateResponse>()
        .WithDescription("Report current playback position");

        group.MapPost("/error", async (ReportErrorRequest request, ReportErrorHandler handler, IEventBroadcaster events, IAnalyticsTracker analytics, INotificationService notifications, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(new ReportErrorCommand(request.Reason), ct);
            analytics.RecordError(request.Reason ?? "unknown");
            events.Broadcast("playback-error", MapState(state));
            _ = notifications.NotifyAsync("playback-error", new { reason = request.Reason, state = state.State.ToString() }, ct);
            return Results.Ok(MapState(state));
        })
        .WithName("ReportError")
        .Produces<PlaybackStateResponse>()
        .WithDescription("Report a playback error");

        app.MapGet("/now-playing", async (GetPlaybackStateHandler handler, CancellationToken ct) =>
        {
            var state = await handler.HandleAsync(ct);
            return Results.Ok(MapState(state));
        })
        .WithTags("Player")
        .WithName("GetNowPlaying")
        .Produces<PlaybackStateResponse>()
        .WithDescription("Get current playback state");
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
                    state.CurrentItem.StartAtSeconds,
                    state.CurrentItem.AddedByUserId,
                    state.CurrentItem.AddedByName)
                : null,
            state.StartedAt,
            state.PositionSeconds,
            state.RetryCount,
            state.LastError);
}
