using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/analytics").WithTags("Analytics");

        group.MapGet("/", (IAnalyticsTracker tracker, DateTimeOffset? from, DateTimeOffset? to) =>
        {
            var snapshot = tracker.GetSnapshot(from, to);
            return Results.Ok(snapshot);
        })
        .WithName("GetAnalytics")
        .WithDescription("Get analytics snapshot with optional time range");

        group.MapGet("/export", (IAnalyticsTracker tracker, DateTimeOffset? from, DateTimeOffset? to) =>
        {
            var snapshot = tracker.GetSnapshot(from, to);
            var json = System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            return Results.Text(json, "application/json");
        })
        .WithName("ExportAnalytics")
        .WithDescription("Export analytics as formatted JSON");
    }
}
