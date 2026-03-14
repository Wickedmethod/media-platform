# MEDIA-756: Player Disk Usage & Cache Cleanup

## Story

**Epic:** MEDIA-PI-OPS — Player Operations & Diagnostics  
**Priority:** Low  
**Effort:** 2 points  
**Status:** ⏳ Planned (🔶 requires Pi hardware for full testing)  
**Depends on:** MEDIA-612 (Local Video Caching), MEDIA-729 (Player Registration)

---

## Summary

Monitor disk usage on Raspberry Pi player nodes and automatically evict cached videos when storage exceeds a configurable threshold. Reports disk metrics to the API for admin visibility.

**Related:** MEDIA-612 implements the video cache. This story manages the cache lifecycle: monitoring, eviction, and reporting.

---

## Disk Monitoring

### Disk Usage Reporting (in heartbeat)

Extend the player heartbeat (MEDIA-724) to include disk metrics:

```json
POST /player/heartbeat
{
  "playerId": "pi-living-room",
  "timestamp": "2026-03-16T14:32:00Z",
  "disk": {
    "totalMb": 29000,
    "usedMb": 18500,
    "cacheMb": 12400,
    "cacheFileCount": 847,
    "usagePercent": 63.8
  }
}
```

### Admin Visibility

`GET /admin/players` includes disk metrics per player:

```json
{
  "id": "pi-living-room",
  "status": "online",
  "disk": {
    "usagePercent": 63.8,
    "cacheMb": 12400,
    "cacheFileCount": 847
  }
}
```

---

## Cache Eviction Strategy

### LRU Eviction

```
Trigger: disk usage > 80% of allocated cache quota
Strategy: Delete least-recently-accessed cached files
Target: Reduce to 70% usage (10% hysteresis to avoid frequent cleanups)
```

### Eviction Rules

| Condition                         | Action                       |
| --------------------------------- | ---------------------------- |
| Cache > 80% quota                 | LRU eviction → reduce to 70% |
| Single file > 500MB               | Never cache (too large)      |
| File age > 30 days without access | Delete regardless of space   |
| Currently playing video           | Never evict                  |
| Videos in queue                   | Prefer keeping, evict last   |

### Configuration

```json
// player-config.json on Pi
{
  "cache": {
    "enabled": true,
    "directory": "/home/pi/.media-platform/cache",
    "maxSizeMb": 20000,
    "highWatermarkPercent": 80,
    "lowWatermarkPercent": 70,
    "maxFileAgeDays": 30,
    "maxSingleFileMb": 500,
    "cleanupIntervalMinutes": 60
  }
}
```

---

## Cleanup Implementation

```typescript
// Player-side (Node.js on Pi)
async function cleanupCache(config: CacheConfig): Promise<CleanupResult> {
  const cacheDir = config.directory;
  const files = await getCacheFiles(cacheDir); // { path, size, lastAccessed }

  const totalSize = files.reduce((sum, f) => sum + f.size, 0);
  const maxSize = config.maxSizeMb * 1024 * 1024;
  const targetSize = maxSize * (config.lowWatermarkPercent / 100);

  if (totalSize <= maxSize * (config.highWatermarkPercent / 100)) {
    return { deleted: 0, freedBytes: 0 };
  }

  // Sort by last accessed (oldest first = LRU)
  const sorted = files.sort((a, b) => a.lastAccessed - b.lastAccessed);

  let currentSize = totalSize;
  let deleted = 0;
  let freedBytes = 0;

  for (const file of sorted) {
    if (currentSize <= targetSize) break;
    if (isCurrentlyPlaying(file) || isInQueue(file)) continue;

    await fs.unlink(file.path);
    currentSize -= file.size;
    freedBytes += file.size;
    deleted++;
  }

  return { deleted, freedBytes };
}
```

---

## Tasks

- [ ] Extend heartbeat payload with disk metrics
- [ ] Add disk info to `GET /admin/players` response
- [ ] Implement LRU cache eviction on Pi player
- [ ] Add high/low watermark configuration
- [ ] Skip currently-playing and queued videos during eviction
- [ ] Add 30-day age-based cleanup
- [ ] Run cleanup on configurable interval (default 60 min)
- [ ] Log eviction events (count, bytes freed)
- [ ] Unit tests for eviction logic (LRU ordering, watermark, exclusions)

---

## Acceptance Criteria

- [ ] Disk usage reported in heartbeat to API
- [ ] Admin can see disk usage per player
- [ ] Cache cleanup triggers at 80% capacity
- [ ] Eviction reduces to 70% (hysteresis)
- [ ] Currently playing video never evicted
- [ ] Files older than 30 days auto-cleaned
