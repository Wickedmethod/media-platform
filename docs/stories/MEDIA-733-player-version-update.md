# MEDIA-733: Player Version & Update Check

## Story

**Epic:** MEDIA-002 — Raspberry Pi Player Node  
**Priority:** Low  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-729 (registration), MEDIA-724 (heartbeat)

---

## Summary

Track player versions and notify admins when players are running outdated software. The API stores the expected version and compares it against heartbeat data. Optionally, the API can broadcast an "update available" message to players.

---

## Version Flow

```
Pi reports version in:
  POST /worker/register  → version: "1.2.0"
  POST /player/heartbeat → version: "1.2.0"
    │
    ▼
API compares against expected version:
  config: expectedVersion = "1.3.0"
    │
    ├── Match: ✅ up to date
    └── Mismatch: ⚠️ "living-room-tv running 1.2.0, expected 1.3.0"
```

---

## Admin Endpoints

### Version Matrix

```
GET /admin/players/versions
Authorization: Bearer <admin-jwt>

{
  "expectedVersion": "1.3.0",
  "players": [
    { "id": "living-room-tv", "version": "1.3.0", "upToDate": true },
    { "id": "bedroom-tv", "version": "1.2.0", "upToDate": false }
  ]
}
```

### Set Expected Version

```
POST /admin/players/expected-version
Authorization: Bearer <admin-jwt>

{ "version": "1.3.0" }
```

### Broadcast Update Notice

```
POST /admin/players/notify-update
Authorization: Bearer <admin-jwt>

{ "message": "Update available: v1.3.0. Run: provision.sh --update" }
```

This sends an SSE event to all connected players:

```json
{ "type": "update-available", "data": { "version": "1.3.0", "message": "..." } }
```

---

## TV Player Update Handler

When the TV app receives `update-available`, it shows a non-intrusive banner:

```
┌──────────────────────────────────────┐
│  ⬆ Update available: v1.3.0         │
│  Run update from admin dashboard     │
└──────────────────────────────────────┘
```

The banner doesn't interrupt playback — it shows during idle or on overlay toggle.

---

## Tasks

- [ ] Add `expectedVersion` config setting (Redis or appsettings)
- [ ] Create `GET /admin/players/versions` endpoint
- [ ] Create `POST /admin/players/expected-version` endpoint
- [ ] Create `POST /admin/players/notify-update` endpoint
- [ ] Emit `update-available` SSE event
- [ ] Add update banner to TV app
- [ ] Unit tests for version comparison logic

---

## Acceptance Criteria

- [ ] Admin can see version matrix for all players
- [ ] Outdated players are clearly flagged
- [ ] Update notification broadcasts to all connected players
- [ ] Update banner shows on TV without interrupting playback
