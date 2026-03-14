# MEDIA-729: Player Startup Registration & Capability Handshake

## Story

**Epic:** MEDIA-003 — Queue and Player API  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-622 (Worker Key auth), MEDIA-724 (heartbeat)  
**Absorbs:** MEDIA-726 (Player Capability Registration)

---

## Summary

When a Pi player node boots, it registers with the API announcing its name, capabilities, and version. The API tracks registered players, enabling the admin dashboard to show "2 players online, 1 offline" and route playback to capable devices.

---

## Registration Flow

```
Pi boots → provision.sh starts services
    │
    POST /worker/register
    X-Worker-Key: <key>
    {
      "name": "Living Room TV",
      "capabilities": { "cec": true, "audio": "hdmi", "maxResolution": "1080p" },
      "version": "1.2.0",
      "os": "Raspberry Pi OS Lite (bookworm)"
    }
    │
    ▼
API stores registration in Redis
    │
    Response 200:
    {
      "playerId": "living-room-tv",
      "serverTime": "2026-03-16T14:00:00Z",
      "config": { "heartbeatInterval": 30, "positionReportInterval": 5 }
    }
    │
    ▼
Pi starts heartbeat loop (MEDIA-724)
Pi connects to SSE /events
```

---

## Endpoints

### Register Player

```
POST /worker/register
X-Worker-Key: <key>

Request:
{
  "name": "Living Room TV",
  "capabilities": {
    "cec": true,
    "audioOutput": "hdmi",
    "maxResolution": "1080p",
    "codecs": ["h264", "vp9"],
    "chromiumVersion": "120.0"
  },
  "version": "1.2.0",
  "os": "Raspberry Pi OS Lite"
}

Response 200:
{
  "playerId": "living-room-tv",
  "serverTime": "2026-03-16T14:00:00Z",
  "config": {
    "heartbeatInterval": 30,
    "positionReportInterval": 5,
    "sseUrl": "/events"
  }
}
```

### List Players (admin)

```
GET /admin/players
Authorization: Bearer <admin-jwt>

Response 200:
[
  {
    "id": "living-room-tv",
    "name": "Living Room TV",
    "isAlive": true,
    "lastSeen": "2026-03-16T14:32:00Z",
    "state": "Playing",
    "version": "1.2.0",
    "capabilities": { "cec": true, "audioOutput": "hdmi" }
  }
]
```

---

## Redis Schema

```
worker:{playerId}  (HASH)
    name           → "Living Room TV"
    registeredAt   → ISO timestamp
    version        → "1.2.0"
    os             → "Raspberry Pi OS Lite"
    capabilities   → JSON string
```

The heartbeat (MEDIA-724) updates liveness separately. Registration is persistent (no TTL) until manually removed.

---

## Tasks

- [ ] Create `POST /worker/register` endpoint (WorkerOnly policy)
- [ ] Create `WorkerRegistration` request/response records
- [ ] Store registration in Redis hash (persistent)
- [ ] Return server-configured intervals (heartbeat, position report)
- [ ] Create `GET /admin/players` (AdminOnly) combining registration + heartbeat data
- [ ] Emit `player-online` SSE event on registration
- [ ] Unit tests for registration storage
- [ ] Integration test for register → list → heartbeat flow

---

## Acceptance Criteria

- [ ] Player registers on startup with name, capabilities, version
- [ ] API returns configuration (intervals, SSE URL)
- [ ] Admin can list all registered players with liveness
- [ ] Registration persists across API restarts (Redis)
- [ ] Duplicate registration (same name) updates existing entry
