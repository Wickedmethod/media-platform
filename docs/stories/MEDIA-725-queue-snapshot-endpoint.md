# MEDIA-725: Queue Snapshot Endpoint for Fast Client Sync

## Story

**Epic:** MEDIA-003 — Queue and Player API  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** None (existing API)

---

## Summary

Add a single `GET /sync` endpoint that returns an atomic snapshot of all client-relevant state in one round-trip: queue, now-playing, player state, queue mode, and active policies. This eliminates the cold-start waterfall where clients make 4+ sequential requests on startup.

---

## Problem

Current startup flow (4 requests):
```
Client boots →
  GET /queue          → 50ms
  GET /now-playing    → 50ms
  GET /queue/mode     → 50ms
  GET /policies       → 50ms
  ────────────────────
  Total: ~200ms (sequential)
```

With snapshot (1 request):
```
Client boots →
  GET /sync           → 60ms
  ────────────────────
  Total: ~60ms
```

---

## Endpoint

```
GET /sync
Authorization: Bearer <jwt> | X-Worker-Key: <key>

Response 200:
{
  "queue": [
    { "id": "abc", "title": "Bohemian Rhapsody", "url": "...", "addedByName": "Jonas" }
  ],
  "nowPlaying": {
    "currentItem": { ... },
    "state": "Playing",
    "position": 142,
    "duration": 354
  },
  "queueMode": "Sequential",
  "policies": [
    { "id": "p1", "type": "MaxDuration", "enabled": true, "config": { "maxSeconds": 600 } }
  ],
  "killSwitch": false,
  "serverTime": "2026-03-16T14:32:00Z",
  "version": 42
}
```

### Version Field

The `version` is an incrementing counter (Redis INCR) bumped on every state mutation. Clients can send `If-None-Match: 42` to get a `304 Not Modified` if nothing changed — useful for polling fallback.

---

## Implementation

```csharp
app.MapGet("/sync", async (IQueueService queue, IPlayerService player, IPolicyService policies, IRedisConnection redis) =>
{
    var (queueItems, nowPlaying, mode, activePolicies, killSwitch, version) = await Task.WhenAll(
        queue.GetAllAsync(),
        player.GetNowPlayingAsync(),
        queue.GetModeAsync(),
        policies.GetAllAsync(),
        player.GetKillSwitchAsync(),
        redis.GetAsync<long>("sync:version")
    );

    return Results.Ok(new SyncSnapshot(queueItems, nowPlaying, mode, activePolicies, killSwitch, version));
})
.WithName("GetSyncSnapshot")
.WithTags("Sync")
.Produces<SyncSnapshot>();
```

---

## Tasks

- [ ] Create `SyncSnapshot` response record
- [ ] Create `GET /sync` endpoint with all state aggregated
- [ ] Add `sync:version` Redis counter, increment on every mutation
- [ ] Support `If-None-Match` / `304 Not Modified` for polling
- [ ] Add OpenAPI metadata to endpoint
- [ ] Integration test for snapshot correctness
- [ ] Integration test for 304 caching

---

## Acceptance Criteria

- [ ] Single request returns queue + now-playing + mode + policies + kill switch
- [ ] Response includes version counter
- [ ] `If-None-Match` returns 304 when state hasn't changed
- [ ] TV and SPA clients can use this for fast startup
