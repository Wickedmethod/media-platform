using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this WebApplication app)
    {
        app.MapGet("/sync", async (
            IQueueRepository repo,
            IKillSwitch killSwitch,
            IPolicyEngine policyEngine,
            HttpContext http,
            CancellationToken ct) =>
        {
            var queueTask = repo.GetQueueAsync(ct);
            var stateTask = repo.GetPlaybackStateAsync(ct);
            var modeTask = repo.GetQueueModeAsync(ct);
            var versionTask = repo.GetVersionAsync(ct);

            await Task.WhenAll(queueTask, stateTask, modeTask, versionTask);

            var version = versionTask.Result;

            // ETag / If-None-Match → 304
            var etag = $"\"{version}\"";
            if (http.Request.Headers.IfNoneMatch.ToString() == etag)
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            var queue = queueTask.Result;
            var state = stateTask.Result;
            var mode = modeTask.Result;
            var policies = policyEngine.GetPolicies();

            var snapshot = new SyncSnapshot(
                queue.Select(i => new QueueItemResponse(
                    i.Id, i.Url.Value, i.Title, i.Status.ToString(),
                    i.AddedAt, i.StartAtSeconds, i.AddedByUserId, i.AddedByName)),
                PlayerEndpoints.MapState(state),
                mode.ToString(),
                policies.Select(p => new PolicySnapshot(p.Id, p.Name, p.Type.ToString(), p.Enabled)).ToList(),
                killSwitch.IsActive,
                DateTimeOffset.UtcNow,
                version);

            http.Response.Headers.ETag = etag;
            return Results.Ok(snapshot);
        })
        .WithName("GetSyncSnapshot")
        .WithTags("Sync")
        .Produces<SyncSnapshot>()
        .Produces(StatusCodes.Status304NotModified)
        .RequireRateLimiting("general")
        .WithDescription("Atomic snapshot of all client-relevant state for fast sync");
    }
}
