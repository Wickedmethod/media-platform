using Prometheus;

namespace MediaPlatform.Infrastructure.Metrics;

/// <summary>
/// Custom Prometheus metrics for the media platform.
/// </summary>
public sealed class MediaPlatformMetrics
{
    private static readonly Gauge QueueDepth = Prometheus.Metrics.CreateGauge(
        "mediaplatform_queue_depth", "Current number of items in the queue");

    private static readonly Gauge PlayerState = Prometheus.Metrics.CreateGauge(
        "mediaplatform_player_state", "Player state (0=Idle, 1=Playing, 2=Paused, 3=Stopped)");

    private static readonly Counter TracksPlayed = Prometheus.Metrics.CreateCounter(
        "mediaplatform_tracks_played_total", "Total tracks played since startup");

    private static readonly Counter PlaybackErrors = Prometheus.Metrics.CreateCounter(
        "mediaplatform_playback_errors_total", "Total playback errors");

    private static readonly Counter QueueAdds = Prometheus.Metrics.CreateCounter(
        "mediaplatform_queue_adds_total", "Total items added to queue");

    private static readonly Gauge ActiveSseConnections = Prometheus.Metrics.CreateGauge(
        "mediaplatform_active_sse_connections", "Number of active SSE client connections");

    private static readonly Gauge ActivePlayers = Prometheus.Metrics.CreateGauge(
        "mediaplatform_active_players", "Number of alive player nodes");

    private static readonly Gauge KillSwitchActive = Prometheus.Metrics.CreateGauge(
        "mediaplatform_kill_switch_active", "1 if kill switch is active, 0 otherwise");

    public void SetQueueDepth(int depth) => QueueDepth.Set(depth);
    public void SetPlayerState(int state) => PlayerState.Set(state);
    public void IncrementTracksPlayed() => TracksPlayed.Inc();
    public void IncrementPlaybackErrors() => PlaybackErrors.Inc();
    public void IncrementQueueAdds() => QueueAdds.Inc();
    public void SetActiveSseConnections(int count) => ActiveSseConnections.Set(count);
    public void SetActivePlayers(int count) => ActivePlayers.Set(count);
    public void SetKillSwitchActive(bool active) => KillSwitchActive.Set(active ? 1 : 0);
}
