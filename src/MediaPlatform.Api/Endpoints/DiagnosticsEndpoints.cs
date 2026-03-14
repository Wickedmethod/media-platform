using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class DiagnosticsEndpoints
{
    public static void MapDiagnosticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/diagnostics").WithTags("Diagnostics");

        group.MapPost("/logs", async (SubmitLogsRequest request, IPlayerLogStore logStore, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.PlayerId))
                return Results.BadRequest(new ApiError("playerId is required"));

            if (request.Entries is null || request.Entries.Count == 0)
                return Results.NoContent();

            var entries = request.Entries.Select(e => new PlayerLogEntry(
                e.Timestamp, e.Level, e.Message, e.Source)).ToList();

            await logStore.AppendLogsAsync(request.PlayerId, entries, ct);
            return Results.NoContent();
        })
        .WithName("SubmitPlayerLogs")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiError>(StatusCodes.Status400BadRequest)
        .WithDescription("Submit a batch of player diagnostic logs");

        // MEDIA-763: Network metrics submission
        group.MapPost("/network", async (SubmitNetworkMetricsRequest request, INetworkMetricsStore store, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.PlayerId))
                return Results.BadRequest(new ApiError("playerId is required"));

            var metrics = new NetworkMetrics(
                request.PlayerId,
                request.Timestamp,
                new LatencyMetrics(request.Latency.AvgMs, request.Latency.MinMs, request.Latency.MaxMs,
                    request.Latency.P95Ms, request.Latency.Samples, request.Latency.Failures),
                new DnsMetrics(request.Dns.AvgResolveMs, request.Dns.Failures),
                new BandwidthMetrics(request.Bandwidth.LastMbps, request.Bandwidth.MeasuredAt));

            await store.SaveMetricsAsync(request.PlayerId, metrics, ct);
            return Results.NoContent();
        })
        .WithName("SubmitNetworkMetrics")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiError>(StatusCodes.Status400BadRequest)
        .WithDescription("Submit aggregated network connectivity metrics from a player");

        // MEDIA-763: Bandwidth test payload (100 KB)
        group.MapGet("/bandwidth-test", () =>
        {
            var payload = new byte[102400]; // 100 KB
            return Results.File(payload, "application/octet-stream");
        })
        .WithName("BandwidthTest")
        .Produces(StatusCodes.Status200OK)
        .WithDescription("Returns a 100 KB payload for player-side bandwidth estimation");
    }
}
