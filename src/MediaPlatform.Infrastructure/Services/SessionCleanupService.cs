using MediaPlatform.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaPlatform.Infrastructure.Services;

public sealed class SessionCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxIdleTime = TimeSpan.FromHours(4);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sessions = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

                var active = await sessions.GetActiveSessionsAsync(stoppingToken);
                var expired = active.Where(s => s.IsExpired(MaxIdleTime)).ToList();

                foreach (var session in expired)
                {
                    await sessions.ClearSessionQueueAsync(session.SessionId, stoppingToken);
                    await sessions.DeleteSessionAsync(session.SessionId, stoppingToken);
                    logger.LogInformation("Cleaned up expired session {SessionId} for user {UserId}",
                        session.SessionId, session.UserId);
                }

                if (expired.Count > 0)
                    logger.LogInformation("Session cleanup: removed {Count} expired sessions", expired.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during session cleanup");
            }
        }
    }
}
