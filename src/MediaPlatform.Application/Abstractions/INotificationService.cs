namespace MediaPlatform.Application.Abstractions;

public interface INotificationService
{
    void RegisterWebhook(string id, Uri url, IReadOnlySet<string> eventTypes);
    void RemoveWebhook(string id);
    IReadOnlyList<WebhookRegistration> GetWebhooks();
    Task NotifyAsync(string eventType, object payload, CancellationToken ct = default);
}

public record WebhookRegistration(string Id, Uri Url, IReadOnlySet<string> EventTypes, DateTimeOffset RegisteredAt);
