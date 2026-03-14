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
    }
}
