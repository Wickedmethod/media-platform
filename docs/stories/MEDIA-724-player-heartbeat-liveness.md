# MEDIA-724: Player Heartbeat & Liveness Reporting

## Story

**Epic:** MEDIA-003 — Queue and Player API  
**Priority:** High  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-622 (Worker Key auth)

---

## Summary

Add a heartbeat endpoint so Pi player nodes report "I'm alive" every 30 seconds. The API tracks the last heartbeat per player and detects zombie players (no heartbeat for >90s). Admins can see player liveness via the dashboard.

---

## New Endpoint

```
POST /player/heartbeat
X-Worker-Key: <key>
Content-Type: application/json

{
  "playerId": "living-room",
  "state": "Playing",
  "position": 142,
  "videoId": "dQw4w9WgXcQ",
  "uptime": 3600,
  "version": "1.0.0"
}
```

**Response:** `204 No Content`

---

## Architecture

```
Pi (every 30s)
    │
    POST /player/heartbeat
    │
    ▼
API stores in Redis:
    player:{playerId}:heartbeat → { lastSeen, state, position, uptime, version }
    │
    ▼
Admin Dashboard reads:
    GET /admin/players → [{ id, lastSeen, state, isAlive, version }]
```

---

## Redis Schema

```
player:{playerId}:heartbeat  (HASH, TTL 120s)
    lastSeen      → ISO timestamp
    state         → "Playing" | "Paused" | "Idle"
    position      → seconds
    videoId       → current video
    uptime        → seconds since boot
    version       → semantic version
```

The TTL auto-expires stale entries — if a player stops sending heartbeats, it disappears after 120s.

---

## Admin Endpoint

```
GET /admin/players
Authorization: Bearer <admin-jwt>

Response:
[
  {
    "id": "living-room",
    "lastSeen": "2026-03-16T14:32:00Z",
    "state": "Playing",
    "isAlive": true,
    "uptime": 3600,
    "version": "1.0.0"
  }
]
```

---

## Zombie Detection

A player is considered a zombie if `lastSeen` > 90s ago:

```csharp
public bool IsAlive(PlayerHeartbeat heartbeat) =>
    (DateTime.UtcNow - heartbeat.LastSeen).TotalSeconds < 90;
```

When a zombie is detected:
1. Mark player as offline in Redis
2. If the zombie was the active player, transition to `Stopped` state
3. Emit SSE event: `player-offline` with player ID

---

## Tasks

- [ ] Create `POST /player/heartbeat` endpoint (WorkerOnly policy)
- [ ] Store heartbeat data in Redis hash with 120s TTL
- [ ] Create `GET /admin/players` endpoint (AdminOnly policy)
- [ ] Add zombie detection logic (>90s = offline)
- [ ] Emit `player-offline` SSE event when zombie detected
- [ ] Unit tests for heartbeat storage and zombie detection
- [ ] Integration test for heartbeat → admin players flow

---

## Acceptance Criteria

- [ ] Player can report heartbeat with state and position
- [ ] Admin can see all players with liveness status
- [ ] Stale players auto-expire from Redis after 120s
- [ ] Zombie detection triggers `player-offline` SSE event
