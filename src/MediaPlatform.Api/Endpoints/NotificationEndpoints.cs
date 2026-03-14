using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/webhooks").WithTags("Notifications");

        group.MapGet("/", (INotificationService svc) =>
        {
            var hooks = svc.GetWebhooks();
            return Results.Ok(hooks.Select(h => new WebhookResponse(h.Id, h.Url.ToString(), h.EventTypes.ToList(), h.RegisteredAt)));
        });

        group.MapPost("/", (RegisterWebhookRequest request, INotificationService svc) =>
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return Results.BadRequest(new ApiError("Invalid webhook URL. Must be an absolute HTTP(S) URL."));
            }

            var events = new HashSet<string>(request.EventTypes ?? []);
            svc.RegisterWebhook(request.Id, uri, events);
            return Results.Created($"/webhooks/{request.Id}", new WebhookResponse(request.Id, uri.ToString(), events.ToList(), DateTimeOffset.UtcNow));
        });

        group.MapDelete("/{id}", (string id, INotificationService svc) =>
        {
            svc.RemoveWebhook(id);
            return Results.NoContent();
        });
    }
}

public record RegisterWebhookRequest(string Id, string Url, List<string>? EventTypes);
public record WebhookResponse(string Id, string Url, List<string> EventTypes, DateTimeOffset RegisteredAt);
