namespace MediaPlatform.Application.Abstractions;

/// <summary>
/// Dispatches anomaly alerts to configured channels (Discord, email, webhooks).
/// </summary>
public interface IAlertDispatcher
{
    Task DispatchAsync(AnomalyAlert alert, CancellationToken ct = default);
}

public sealed class AlertingOptions
{
    public bool Enabled { get; set; }
    public int CooldownMinutes { get; set; } = 5;
    public List<AlertChannelConfig> Channels { get; set; } = [];
}

public sealed class AlertChannelConfig
{
    public string Type { get; set; } = string.Empty;        // "Discord" | "Email"
    public string? WebhookUrl { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 25;
    public string? To { get; set; }
    public string? From { get; set; }
    public string MinSeverity { get; set; } = "Warning";    // "Warning" | "Critical"
}
