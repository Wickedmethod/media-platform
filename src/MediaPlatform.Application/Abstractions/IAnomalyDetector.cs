namespace MediaPlatform.Application.Abstractions;

/// <summary>
/// Detects anomalous patterns (spike in denied requests, unusual command volumes).
/// </summary>
public interface IAnomalyDetector
{
    void RecordRequest(string endpoint, bool denied, string? userId = null);
    AnomalyReport Evaluate();
}

public record AnomalyReport(
    bool HasAnomalies,
    IReadOnlyList<AnomalyAlert> Alerts);

public record AnomalyAlert(
    string RuleName,
    string Description,
    string Severity, // "critical", "warning", "info"
    DateTimeOffset DetectedAt);
