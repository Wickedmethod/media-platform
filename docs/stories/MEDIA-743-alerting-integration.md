# MEDIA-743: Alerting Integration for Anomalies

## Story

**Epic:** Infrastructure & Security  
**Priority:** Low  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-631 (Anomaly Detection), MEDIA-613 (Webhooks)

---

## Summary

Extend the existing anomaly detection system (MEDIA-631) to dispatch alerts to external services: Discord webhooks, Slack, or email. When anomalies are detected (unusual request spikes, repeated errors, suspicious patterns), admins get notified immediately without checking the dashboard.

---

## Architecture

```
Anomaly Detector (MEDIA-631)
    │ detects anomaly
    ▼
Alert Dispatcher
    │
    ├── Discord webhook  → #media-alerts channel
    ├── Email (SMTP)     → admin@homelab.local
    └── Webhook (custom) → existing webhook system (MEDIA-613)
```

---

## Alert Configuration

```json
// appsettings.json
{
  "Alerting": {
    "Channels": [
      {
        "Type": "Discord",
        "WebhookUrl": "https://discord.com/api/webhooks/...",
        "MinSeverity": "Warning"
      },
      {
        "Type": "Email",
        "SmtpHost": "smtp.homelab.local",
        "To": "admin@homelab.local",
        "MinSeverity": "Critical"
      }
    ],
    "CooldownMinutes": 5,
    "Enabled": true
  }
}
```

### Cooldown

To prevent alert fatigue, alerts of the same type are throttled:
- Same anomaly type → max 1 alert per 5 minutes
- Different anomaly types → each has its own cooldown

---

## Discord Alert Format

```json
{
  "embeds": [{
    "title": "⚠️ Media Platform Anomaly",
    "description": "Unusual request spike detected",
    "color": 16744448,
    "fields": [
      { "name": "Type", "value": "RequestSpike", "inline": true },
      { "name": "Severity", "value": "Warning", "inline": true },
      { "name": "Details", "value": "42 requests in 60s (threshold: 20)" },
      { "name": "Source IP", "value": "192.168.1.42" }
    ],
    "timestamp": "2026-03-16T14:32:00Z"
  }]
}
```

---

## Implementation

```csharp
public class AlertDispatcher(IOptions<AlertingOptions> options, IHttpClientFactory httpFactory)
{
    public async Task DispatchAsync(AnomalyEvent anomaly)
    {
        if (!options.Value.Enabled) return;
        if (IsInCooldown(anomaly.Type)) return;

        foreach (var channel in options.Value.Channels)
        {
            if (anomaly.Severity < channel.MinSeverity) continue;

            await channel.Type switch
            {
                "Discord" => SendDiscordAlert(channel, anomaly),
                "Email" => SendEmailAlert(channel, anomaly),
                _ => Task.CompletedTask,
            };
        }

        SetCooldown(anomaly.Type);
    }
}
```

---

## Tasks

- [ ] Create `AlertDispatcher` service
- [ ] Implement Discord webhook alert sender
- [ ] Implement email (SMTP) alert sender
- [ ] Add cooldown logic (per anomaly type)
- [ ] Configure alert channels in `appsettings.json`
- [ ] Wire `AlertDispatcher` into existing anomaly detection pipeline
- [ ] Unit tests for cooldown logic and message formatting
- [ ] Manual test with Discord webhook

---

## Acceptance Criteria

- [ ] Anomalies trigger Discord alert with severity, type, details
- [ ] Alert cooldown prevents duplicate alerts within 5 minutes
- [ ] Alerts can be disabled via configuration
- [ ] Multiple alert channels supported simultaneously
