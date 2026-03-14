using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using MediaPlatform.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediaPlatform.Infrastructure.Alerting;

public sealed class AlertDispatcher(
    IHttpClientFactory httpFactory,
    IOptions<AlertingOptions> options,
    ILogger<AlertDispatcher> logger) : IAlertDispatcher
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cooldowns = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task DispatchAsync(AnomalyAlert alert, CancellationToken ct = default)
    {
        var config = options.Value;
        if (!config.Enabled) return;
        if (IsInCooldown(alert.RuleName, config.CooldownMinutes)) return;

        foreach (var channel in config.Channels)
        {
            if (!MeetsSeverity(alert.Severity, channel.MinSeverity)) continue;

            try
            {
                switch (channel.Type)
                {
                    case "Discord":
                        await SendDiscordAlertAsync(channel, alert, ct);
                        break;
                    // Email and other channels can be added later
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send alert via {Channel} for {Rule}", channel.Type, alert.RuleName);
            }
        }

        SetCooldown(alert.RuleName);
    }

    private async Task SendDiscordAlertAsync(AlertChannelConfig channel, AnomalyAlert alert, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(channel.WebhookUrl)) return;

        var color = alert.Severity == "critical" ? 15158332 : 16744448; // red : orange
        var emoji = alert.Severity == "critical" ? "🚨" : "⚠️";

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = $"{emoji} Media Platform Anomaly",
                    description = alert.Description,
                    color,
                    fields = new[]
                    {
                        new { name = "Type", value = alert.RuleName, inline = true },
                        new { name = "Severity", value = alert.Severity, inline = true },
                        new { name = "Details", value = alert.Description, inline = false }
                    },
                    timestamp = alert.DetectedAt.ToString("o")
                }
            }
        };

        using var client = httpFactory.CreateClient("alerts");
        using var response = await client.PostAsJsonAsync(channel.WebhookUrl, payload, JsonOpts, ct);
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Discord alert sent for {Rule}", alert.RuleName);
    }

    private bool IsInCooldown(string ruleName, int cooldownMinutes)
    {
        if (_cooldowns.TryGetValue(ruleName, out var lastSent))
        {
            return DateTimeOffset.UtcNow - lastSent < TimeSpan.FromMinutes(cooldownMinutes);
        }
        return false;
    }

    private void SetCooldown(string ruleName)
    {
        _cooldowns[ruleName] = DateTimeOffset.UtcNow;
    }

    private static bool MeetsSeverity(string alertSeverity, string minSeverity)
    {
        var severityRank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["info"] = 0,
            ["warning"] = 1,
            ["critical"] = 2
        };

        var alertRank = severityRank.GetValueOrDefault(alertSeverity, 0);
        var minRank = severityRank.GetValueOrDefault(minSeverity, 0);
        return alertRank >= minRank;
    }
}
