using MediaPlatform.Api.Authorization;
using MediaPlatform.Application.Abstractions;

namespace MediaPlatform.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/webhooks").WithTags("Notifications").RequireAuthorization(AuthPolicies.AdminOnly);

        group.MapGet("/", (INotificationService svc) =>
        {
            var hooks = svc.GetWebhooks();
            return Results.Ok(hooks.Select(h => new WebhookResponse(h.Id, h.Url.ToString(), h.EventTypes.ToList(), h.RegisteredAt)));
        })
        .WithName("GetWebhooks")
        .WithDescription("List all registered webhooks");

        group.MapPost("/", (RegisterWebhookRequest request, INotificationService svc) =>
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return Results.BadRequest(new ApiError("Invalid webhook URL. Must be an absolute HTTP(S) URL."));
            }

            var id = request.Id ?? Guid.NewGuid().ToString("N")[..8];
            var events = new HashSet<string>(request.EventTypes ?? []);
            svc.RegisterWebhook(id, uri, events);
            return Results.Created($"/webhooks/{id}", new WebhookResponse(id, uri.ToString(), events.ToList(), DateTimeOffset.UtcNow));
        })
        .WithName("RegisterWebhook")
        .Produces<WebhookResponse>(StatusCodes.Status201Created)
        .Produces<ApiError>(StatusCodes.Status400BadRequest)
        .WithDescription("Register a new webhook");

        group.MapDelete("/{id}", (string id, INotificationService svc) =>
        {
            svc.RemoveWebhook(id);
            return Results.NoContent();
        })
        .WithName("RemoveWebhook")
        .Produces(StatusCodes.Status204NoContent)
        .WithDescription("Remove a webhook by ID");
    }
}

public record RegisterWebhookRequest(string? Id, string Url, List<string>? EventTypes);
public record WebhookResponse(string Id, string Url, List<string> EventTypes, DateTimeOffset RegisteredAt);
