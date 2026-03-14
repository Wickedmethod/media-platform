using MediaPlatform.Api.Authorization;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class EventStreamEndpoints
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    public static void MapEventStreamEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events", async (IEventBroadcaster broadcaster, HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            await ctx.Response.WriteAsync("retry: 3000\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);

            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ = SendHeartbeatsAsync(ctx, heartbeatCts.Token);

            await foreach (var (eventType, json) in broadcaster.SubscribeAsync(ct))
            {
                await ctx.Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        }).WithTags("Events").RequireAuthorization(AuthPolicies.ReadAccess).ExcludeFromDescription();
    }

    private static async Task SendHeartbeatsAsync(HttpContext ctx, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await ctx.Response.WriteAsync("event: heartbeat\ndata: {}\n\n", ct);
            await ctx.Response.Body.FlushAsync(ct);
        }
    }
}
