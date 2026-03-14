using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using MediaPlatform.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace MediaPlatform.Infrastructure.Notifications;

public sealed class WebhookNotificationService : INotificationService
{
    private readonly ConcurrentDictionary<string, WebhookRegistration> _webhooks = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookNotificationService> _logger;
    private const int MaxRetries = 3;

    public WebhookNotificationService(IHttpClientFactory httpClientFactory, ILogger<WebhookNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public void RegisterWebhook(string id, Uri url, IReadOnlySet<string> eventTypes)
    {
        var registration = new WebhookRegistration(id, url, eventTypes, DateTimeOffset.UtcNow);
        _webhooks[id] = registration;
        _logger.LogInformation("Webhook registered: {Id} -> {Url} for events [{Events}]", id, url, string.Join(", ", eventTypes));
    }

    public void RemoveWebhook(string id)
    {
        _webhooks.TryRemove(id, out _);
        _logger.LogInformation("Webhook removed: {Id}", id);
    }

    public IReadOnlyList<WebhookRegistration> GetWebhooks() =>
        _webhooks.Values.ToList().AsReadOnly();

    public async Task NotifyAsync(string eventType, object payload, CancellationToken ct = default)
    {
        var matching = _webhooks.Values
            .Where(w => w.EventTypes.Count == 0 || w.EventTypes.Contains(eventType))
            .ToList();

        if (matching.Count == 0) return;

        var json = JsonSerializer.Serialize(new { eventType, payload, timestamp = DateTimeOffset.UtcNow });

        await Parallel.ForEachAsync(matching, ct, async (webhook, token) =>
        {
            await SendWithRetryAsync(webhook, json, token);
        });
    }

    private async Task SendWithRetryAsync(WebhookRegistration webhook, string json, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("webhooks");
        client.Timeout = TimeSpan.FromSeconds(10);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(webhook.Url, content, ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Webhook {Id} delivered successfully", webhook.Id);
                    return;
                }
                _logger.LogWarning("Webhook {Id} attempt {Attempt} returned {Status}", webhook.Id, attempt, response.StatusCode);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Webhook {Id} attempt {Attempt} failed", webhook.Id, attempt);
            }

            if (attempt < MaxRetries)
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
        }

        _logger.LogError("Webhook {Id} delivery failed after {MaxRetries} attempts", webhook.Id, MaxRetries);
    }
}
