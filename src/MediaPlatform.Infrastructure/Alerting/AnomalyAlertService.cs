using MediaPlatform.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediaPlatform.Infrastructure.Alerting;

/// <summary>
/// Background service that periodically evaluates anomaly detection
/// and dispatches alerts when thresholds are exceeded.
/// </summary>
public sealed class AnomalyAlertService(
    IAnomalyDetector detector,
    IAlertDispatcher dispatcher,
    IOptions<AlertingOptions> options,
    ILogger<AnomalyAlertService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Anomaly alerting is disabled");
            return;
        }

        logger.LogInformation("Anomaly alerting started with {Channels} channel(s), {Cooldown}min cooldown",
            options.Value.Channels.Count, options.Value.CooldownMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckInterval, stoppingToken);

                var report = detector.Evaluate();
                if (!report.HasAnomalies) continue;

                foreach (var alert in report.Alerts)
                {
                    await dispatcher.DispatchAsync(alert, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during anomaly alert check");
            }
        }
    }
}
