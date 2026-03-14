# MEDIA-763: Player Network Connectivity Monitoring

## Story

**Epic:** MEDIA-PI-OPS — Player Operations & Diagnostics  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-729 (Player Registration), MEDIA-732 (Player Log Streaming)

---

## Summary

Add active network diagnostics to the Raspberry Pi player so the API can monitor network health beyond simple "is it alive?" heartbeats. Measure latency to the API, DNS resolution time, and estimated bandwidth — then report metrics periodically. This helps diagnose buffering issues, intermittent connectivity, and network degradation before they cause playback failures.

**Note:** This is distinct from heartbeat (MEDIA-724) which checks liveness, and SSE reconnect (MEDIA-734) which handles connection recovery. This story adds proactive network quality measurement.

---

## Architecture

```
Raspberry Pi Player
    │
    ├─ Latency Probe     → ping API /health/live, measure RTT
    ├─ DNS Health Check   → resolve API hostname, measure time
    ├─ Bandwidth Estimate → timed download of known payload from API
    │
    ▼
Metrics Aggregator (local, 1-min rolling window)
    │
    ▼
POST /diagnostics/network  (every 60s)
    │
    ▼
API stores in Redis → available via GET /admin/players/{id}/network
```

---

## Network Probes

### 1. Latency Probe

```typescript
// player/src/network/latency-probe.ts
export async function measureLatency(apiBaseUrl: string): Promise<number> {
  const start = performance.now();
  const response = await fetch(`${apiBaseUrl}/health/live`, {
    method: "GET",
    signal: AbortSignal.timeout(5000),
  });
  const rtt = performance.now() - start;

  if (!response.ok) throw new Error(`Health check failed: ${response.status}`);
  return Math.round(rtt);
}
```

### 2. DNS Health Check

```typescript
// player/src/network/dns-probe.ts
import { promises as dns } from "node:dns";

export async function measureDns(hostname: string): Promise<{
  resolveTimeMs: number;
  addresses: string[];
}> {
  const start = performance.now();
  const addresses = await dns.resolve4(hostname);
  const resolveTimeMs = Math.round(performance.now() - start);

  return { resolveTimeMs, addresses };
}
```

### 3. Bandwidth Estimation

```typescript
// player/src/network/bandwidth-probe.ts
export async function estimateBandwidth(apiBaseUrl: string): Promise<{
  downloadMbps: number;
  payloadBytes: number;
  durationMs: number;
}> {
  // API serves a known-size payload (e.g., 100KB of zeros)
  const start = performance.now();
  const response = await fetch(`${apiBaseUrl}/diagnostics/bandwidth-test`, {
    signal: AbortSignal.timeout(10000),
  });
  const blob = await response.blob();
  const durationMs = performance.now() - start;

  const payloadBytes = blob.size;
  const downloadMbps = (payloadBytes * 8) / (durationMs * 1000); // bits/ms → Mbps

  return {
    downloadMbps: Math.round(downloadMbps * 100) / 100,
    payloadBytes,
    durationMs: Math.round(durationMs),
  };
}
```

---

## Metrics Aggregator

```typescript
// player/src/network/metrics-aggregator.ts
interface NetworkMetrics {
  timestamp: string;
  latency: {
    avgMs: number;
    minMs: number;
    maxMs: number;
    p95Ms: number;
    samples: number;
    failures: number;
  };
  dns: {
    avgResolveMs: number;
    failures: number;
  };
  bandwidth: {
    lastMbps: number;
    measuredAt: string;
  };
}
```

### Collection Schedule

| Probe         | Interval    | Timeout | Notes                          |
| ------------- | ----------- | ------- | ------------------------------ |
| Latency       | Every 15s   | 5s      | Lightweight, uses /health/live |
| DNS           | Every 60s   | 5s      | Minimal overhead               |
| Bandwidth     | Every 5 min | 10s     | Heavier, runs less frequently  |
| Report to API | Every 60s   | 5s      | Aggregated metrics batch       |

---

## API Endpoints

### POST /diagnostics/network

Player submits aggregated network metrics:

```json
{
  "playerId": "pi-living-room",
  "timestamp": "2027-01-15T10:30:00Z",
  "latency": {
    "avgMs": 12,
    "minMs": 8,
    "maxMs": 45,
    "p95Ms": 28,
    "samples": 4,
    "failures": 0
  },
  "dns": {
    "avgResolveMs": 3,
    "failures": 0
  },
  "bandwidth": {
    "lastMbps": 85.2,
    "measuredAt": "2027-01-15T10:25:00Z"
  }
}
```

### GET /admin/players/{id}/network

Returns latest network metrics + trend (last 1h):

```json
{
  "current": { ... },
  "trend": {
    "latencyTrend": "stable",    // stable | degrading | improving
    "avgLatency1h": 14,
    "bandwidthTrend": "stable",
    "avgBandwidth1h": 82.5
  }
}
```

### GET /diagnostics/bandwidth-test

Returns a fixed-size payload (100 KB) for bandwidth estimation:

```csharp
[HttpGet("bandwidth-test")]
public IActionResult BandwidthTest()
{
    var payload = new byte[102400]; // 100 KB
    return File(payload, "application/octet-stream");
}
```

---

## Alert Thresholds

| Metric         | Warning         | Critical        | Action                     |
| -------------- | --------------- | --------------- | -------------------------- |
| Latency avg    | > 200ms         | > 500ms         | Log warning / alert        |
| Latency p95    | > 500ms         | > 1000ms        | Alert                      |
| DNS resolve    | > 100ms         | > 500ms         | Alert + check DNS config   |
| Bandwidth      | < 10 Mbps       | < 5 Mbps        | Alert — buffering likely   |
| Probe failures | > 2 consecutive | > 5 consecutive | Alert — connectivity issue |

Alerts integrate with the existing anomaly detection system (MEDIA-631, MEDIA-743).

---

## Redis Storage

```
network:metrics:{playerId}        → latest NetworkMetrics (JSON)
network:metrics:{playerId}:history → sorted set (score=timestamp, member=JSON)
    TTL: 24 hours (auto-expire old data)
```

---

## Tasks

- [ ] Implement latency probe (fetch /health/live, measure RTT)
- [ ] Implement DNS health check probe (dns.resolve4, measure time)
- [ ] Implement bandwidth estimation probe (timed download)
- [ ] Create metrics aggregator with rolling window (1-min aggregation)
- [ ] Build collection scheduler with configurable intervals per probe
- [ ] Create `POST /diagnostics/network` endpoint on API
- [ ] Create `GET /admin/players/{id}/network` endpoint with trend data
- [ ] Create `GET /diagnostics/bandwidth-test` endpoint (100 KB payload)
- [ ] Store metrics in Redis with 24h TTL
- [ ] Add alert thresholds for latency, DNS, bandwidth degradation
- [ ] Integrate alerts with existing anomaly detection (MEDIA-743)
- [ ] Write tests for metric aggregation and trend calculation
- [ ] Write tests for probe failure handling and timeout

---

## Acceptance Criteria

- [ ] Latency measured every 15s against `/health/live`
- [ ] DNS resolution time measured every 60s
- [ ] Bandwidth estimated every 5 minutes
- [ ] Aggregated metrics reported to API every 60s
- [ ] API stores metrics in Redis with 24h retention
- [ ] Admin endpoint returns current metrics + 1h trend
- [ ] Alerts fire when latency > 500ms or bandwidth < 5 Mbps
- [ ] Probe failures counted and alerted after 5 consecutive failures
- [ ] Probes don't block playback (run in background, respect timeouts)
- [ ] Network diagnostics visible in admin player detail view
