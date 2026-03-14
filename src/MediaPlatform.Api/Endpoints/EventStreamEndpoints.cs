using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class EventStreamEndpoints
{
    public static void MapEventStreamEndpoints(this WebApplication app)
    {
        app.MapGet("/events", async (IEventBroadcaster broadcaster, HttpContext ctx, CancellationToken ct) =>
        {
            ctx.Response.Headers.ContentType = "text/event-stream";
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers.Connection = "keep-alive";

            await ctx.Response.Body.FlushAsync(ct);

            await foreach (var (eventType, json) in broadcaster.SubscribeAsync(ct))
            {
                await ctx.Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        }).WithTags("Events").ExcludeFromDescription();
    }
}
