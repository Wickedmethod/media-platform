# MEDIA-742: Metrics Export — Prometheus Format

## Story

**Epic:** Infrastructure & Security  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** None (existing API)

---

## Summary

Expose a `/metrics` endpoint in Prometheus scrape format so the media platform can be monitored via Prometheus + Grafana. Export HTTP request metrics, Redis connection health, queue depth, playback stats, and .NET runtime metrics.

---

## Metrics to Export

### Application Metrics

| Metric                                 | Type    | Description                                            |
| -------------------------------------- | ------- | ------------------------------------------------------ |
| `mediaplatform_queue_depth`            | Gauge   | Current number of items in queue                       |
| `mediaplatform_player_state`           | Gauge   | Current state (0=Idle, 1=Playing, 2=Paused, 3=Stopped) |
| `mediaplatform_tracks_played_total`    | Counter | Total tracks played since startup                      |
| `mediaplatform_playback_errors_total`  | Counter | Total playback errors                                  |
| `mediaplatform_queue_adds_total`       | Counter | Total items added to queue                             |
| `mediaplatform_active_sse_connections` | Gauge   | Number of active SSE clients                           |
| `mediaplatform_active_players`         | Gauge   | Number of alive player nodes                           |
| `mediaplatform_kill_switch_active`     | Gauge   | 1 if kill switch is on, 0 otherwise                    |

### HTTP Metrics (automatic via prometheus-net)

| Metric                          | Type      | Description                            |
| ------------------------------- | --------- | -------------------------------------- |
| `http_requests_total`           | Counter   | Total requests by method, path, status |
| `http_request_duration_seconds` | Histogram | Request latency distribution           |

### Runtime Metrics (automatic)

.NET GC stats, thread pool, memory usage — provided by `prometheus-net`.

---

## Implementation

### NuGet Packages

```xml
<PackageReference Include="prometheus-net.AspNetCore" Version="9.0.0" />
```

### Program.cs

```csharp
// Metrics
builder.Services.AddSingleton<MediaPlatformMetrics>();

var app = builder.Build();

// HTTP metrics middleware
app.UseHttpMetrics();

// Metrics endpoint
app.MapMetrics(); // GET /metrics
```

### Custom Metrics Class

```csharp
public class MediaPlatformMetrics
{
    private static readonly Gauge QueueDepth = Metrics.CreateGauge(
        "mediaplatform_queue_depth", "Current queue depth");

    private static readonly Gauge PlayerState = Metrics.CreateGauge(
        "mediaplatform_player_state", "Player state (0=Idle,1=Playing,2=Paused,3=Stopped)");

    private static readonly Counter TracksPlayed = Metrics.CreateCounter(
        "mediaplatform_tracks_played_total", "Total tracks played");

    private static readonly Gauge ActiveSSE = Metrics.CreateGauge(
        "mediaplatform_active_sse_connections", "Active SSE connections");

    public void SetQueueDepth(int depth) => QueueDepth.Set(depth);
    public void SetPlayerState(int state) => PlayerState.Set(state);
    public void IncrementTracksPlayed() => TracksPlayed.Inc();
    public void SetActiveSSE(int count) => ActiveSSE.Set(count);
}
```

---

## Prometheus Scrape Config

```yaml
# prometheus.yml
scrape_configs:
  - job_name: "media-platform"
    scrape_interval: 15s
    static_configs:
      - targets: ["media-platform-api:5000"]
```

---

## Tasks

- [ ] Add `prometheus-net.AspNetCore` NuGet package
- [ ] Create `MediaPlatformMetrics` class with custom gauges/counters
- [ ] Register metrics in DI and instrument key code paths
- [ ] Add `app.UseHttpMetrics()` and `app.MapMetrics()`
- [ ] Update queue/player services to update metrics on mutations
- [ ] Verify `/metrics` returns valid Prometheus format
- [ ] Add Prometheus scrape config example to docs

---

## Acceptance Criteria

- [ ] `GET /metrics` returns Prometheus text format
- [ ] Queue depth gauge reflects actual queue size
- [ ] HTTP request metrics (count, latency) exported
- [ ] .NET runtime metrics (GC, threads, memory) exported
- [ ] Metrics update in real-time as state changes
