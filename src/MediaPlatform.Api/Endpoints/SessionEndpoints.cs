using System.Security.Claims;
using MediaPlatform.Api.Authorization;
using MediaPlatform.Application.Abstractions;
using MediaPlatform.Application.Commands;
using MediaPlatform.Application.Queries;
using MediaPlatform.Application.Validation;
using MediaPlatform.Domain.Enums;
using MediaPlatform.Domain.Errors;

namespace MediaPlatform.Api.Endpoints;

public static class SessionEndpoints
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sessions").WithTags("Sessions").RequireRateLimiting("general");

        group.MapPost("/personal", async (
            CreateSessionRequest request,
            CreatePersonalSessionHandler handler,
            HttpContext http, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            var session = await handler.HandleAsync(
                new CreatePersonalSessionCommand(userId, request.DeviceId), ct);
            return Results.Created($"/sessions/{session.SessionId}", MapSession(session));
        })
        .WithName("CreatePersonalSession")
        .Produces<SessionResponse>(StatusCodes.Status201Created)
        .RequireAuthorization(AuthPolicies.ReadAccess)
        .WithDescription("Create or resume a personal audio session");

        group.MapGet("/mine", async (
            GetMySessionHandler handler,
            HttpContext http, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            var snapshot = await handler.HandleAsync(new GetMySessionQuery(userId), ct);
            if (snapshot is null)
                return Results.NotFound(new ApiError("No active session"));

            return Results.Ok(new SessionSnapshotResponse(
                MapSession(snapshot.Session),
                snapshot.Queue.Select(QueueEndpoints.MapItem),
                PlayerEndpoints.MapState(snapshot.Playback)));
        })
        .WithName("GetMySession")
        .Produces<SessionSnapshotResponse>()
        .Produces<ApiError>(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthPolicies.ReadAccess)
        .WithDescription("Get the current user's personal session with queue and playback state");

        group.MapPost("/{sessionId}/queue/add", async (
            string sessionId,
            AddToSessionQueueRequest request,
            AddToSessionQueueHandler handler,
            ISessionEventBroadcaster events,
            ISessionRepository sessions,
            HttpContext http, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            await ValidateSessionOwnership(sessions, sessionId, userId, ct);

            var (url, title) = QueueItemSanitizer.Sanitize(request.Url, request.Title);
            var validation = QueueItemValidator.Validate(url, title);
            if (!validation.IsValid)
                return Results.BadRequest(new ApiError(validation.Error!));

            var userName = http.User.FindFirst("preferred_username")?.Value
                ?? http.User.FindFirst(ClaimTypes.Name)?.Value;

            var item = await handler.HandleAsync(
                new AddToSessionQueueCommand(sessionId, url, title ?? string.Empty,
                    request.StartAtSeconds, userId, userName), ct);

            events.Broadcast(sessionId, "session-queue-updated",
                new SseEvents.QueueUpdated("add"));

            return Results.Created($"/sessions/{sessionId}/queue", QueueEndpoints.MapItem(item));
        })
        .WithName("AddToSessionQueue")
        .Produces<QueueItemResponse>(StatusCodes.Status201Created)
        .Produces<ApiError>(StatusCodes.Status400BadRequest)
        .Produces<ApiError>(StatusCodes.Status403Forbidden)
        .RequireAuthorization(AuthPolicies.ReadAccess)
        .WithDescription("Add an item to a session's queue");

        group.MapPost("/{sessionId}/player/{action}", async (
            string sessionId,
            string action,
            SessionPlayerCommandHandler handler,
            ISessionEventBroadcaster events,
            ISessionRepository sessions,
            HttpContext http, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            await ValidateSessionOwnership(sessions, sessionId, userId, ct);

            if (!Enum.TryParse<CommandType>(action, true, out var command))
                return Results.BadRequest(new ApiError($"Invalid action: {action}. Valid: play, pause, skip, stop"));

            try
            {
                var state = await handler.HandleAsync(new SessionPlayerCommand(sessionId, command), ct);
                var response = PlayerEndpoints.MapState(state);
                events.Broadcast(sessionId, "session-playback-state", response);
                return Results.Ok(response);
            }
            catch (InvalidStateTransitionException ex)
            {
                return Results.Conflict(new ApiError(ex.Message));
            }
        })
        .WithName("SessionPlayerCommand")
        .Produces<PlaybackStateResponse>()
        .Produces<ApiError>(StatusCodes.Status400BadRequest)
        .Produces<ApiError>(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthPolicies.ReadAccess)
        .WithDescription("Control session playback (play, pause, skip, stop)");

        group.MapDelete("/{sessionId}", async (
            string sessionId,
            EndSessionHandler handler,
            ISessionEventBroadcaster events,
            HttpContext http, CancellationToken ct) =>
        {
            var userId = GetUserId(http);
            await handler.HandleAsync(new EndSessionCommand(sessionId, userId), ct);
            events.Broadcast(sessionId, "session-ended", new { sessionId });
            return Results.NoContent();
        })
        .WithName("EndSession")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiError>(StatusCodes.Status403Forbidden)
        .RequireAuthorization(AuthPolicies.ReadAccess)
        .WithDescription("End a personal session");

        group.MapGet("/{sessionId}/events", async (
            string sessionId,
            ISessionEventBroadcaster broadcaster,
            ISessionRepository sessions,
            HttpContext ctx, CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            await ValidateSessionOwnership(sessions, sessionId, userId, ct);

            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            await ctx.Response.WriteAsync("retry: 3000\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);

            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = SendHeartbeatsAsync(ctx, heartbeatCts.Token);

            await foreach (var (eventType, json) in broadcaster.SubscribeAsync(sessionId, ct))
            {
                await ctx.Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        })
        .WithTags("Sessions")
        .RequireAuthorization(AuthPolicies.ReadAccess)
        .ExcludeFromDescription();
    }

    private static string GetUserId(HttpContext http) =>
        http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID not found");

    private static async Task ValidateSessionOwnership(
        ISessionRepository sessions, string sessionId, string userId, CancellationToken ct)
    {
        var session = await sessions.GetSessionAsync(sessionId, ct)
            ?? throw new InvalidOperationException("Session not found");

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Not your session");
    }

    private static SessionResponse MapSession(Domain.Entities.PlaybackSession s) =>
        new(s.SessionId, s.UserId, s.DeviceId, s.Type.ToString(), s.CreatedAt, s.LastActivityAt);

    private static async Task SendHeartbeatsAsync(HttpContext ctx, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await ctx.Response.WriteAsync("event: heartbeat\ndata: {}\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);
        }
    }

    internal static QueueItemResponse MapItem(Domain.Entities.QueueItem item) =>
        QueueEndpoints.MapItem(item);
}
