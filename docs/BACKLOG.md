# Media Platform — Backlog

> Last updated: 2026-03-14

## Legend

| Status    | Meaning |
|-----------|---------|
| ✅ Done   | Implemented, tested, merged |
| 🔶 Blocked | Requires external service (Keycloak, Vault, Google OAuth, Raspberry Pi) |
| ⏳ Planned | Can be implemented independently |

## Summary

- **21 stories done** (all tested, committed)
- **107 tests** (83 unit + 24 integration), 0 failures
- **24 stories blocked** by external dependencies
- **0 stories remaining** that can be implemented without infrastructure

---

## Epic: MEDIA-001 — Homelab Media Platform

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-633 | Account Compromise Runbook | ✅ Done | `docs/COMPROMISE-RUNBOOK.md` |

## Epic: MEDIA-002 — Raspberry Pi Player Node

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-201 | Install Raspberry Pi OS | 🔶 Blocked | Needs Pi hardware |
| MEDIA-202 | Install Playback Dependencies | 🔶 Blocked | Needs Pi hardware |
| MEDIA-203 | Implement Player Worker | 🔶 Blocked | Needs Pi + Redis on Pi |
| MEDIA-204 | Create Systemd Service | 🔶 Blocked | Needs Pi hardware |
| MEDIA-205 | HDMI + CEC Remote Support | 🔶 Blocked | Needs Pi + Samsung TV |
| MEDIA-612 | Local Video Caching | 🔶 Blocked | Depends on MEDIA-202 |

## Epic: MEDIA-003 — Queue and Player API

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-003 | Queue & Player API | ✅ Done | Core REST endpoints |
| MEDIA-601 | Queue State Machine | ✅ Done | Centralized state transitions |
| MEDIA-602 | Retry & Failure Handling | ✅ Done | 3x retry, auto-skip |
| MEDIA-607 | Idempotent Player Commands | ✅ Done | No-op on duplicate states |
| MEDIA-608 | Resume Playback | ✅ Done | Position tracking + StartAtSeconds |
| MEDIA-609 | Smart Queue Modes | ✅ Done | Normal / Shuffle / PlayNext |
| MEDIA-613 | Event Notifications | ✅ Done | Webhook system with retry |
| MEDIA-614 | Usage Analytics | ✅ Done | Command latency, errors, playback time |
| MEDIA-618 | SSE Realtime Transport | ✅ Done | `/events` endpoint, auto-reconnect |
| MEDIA-620 | Standardized API Errors | ✅ Done | `ApiError` record, consistent 4xx |
| MEDIA-624 | API Contract Tests | ✅ Done | 20 integration tests via WebApplicationFactory |
| MEDIA-610 | Multi-User Voting | 🔶 Blocked | Needs Keycloak auth |
| MEDIA-630 | YouTube Action Approval | 🔶 Blocked | Needs Keycloak roles |
| MEDIA-631 | Security Alerts & Anomaly Detection | ✅ Done | Sliding window anomaly detector, webhook alerts |

## Epic: MEDIA-004 — YouTube Integration

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-401 | Google OAuth Login | 🔶 Blocked | Needs Google API credentials |
| MEDIA-402 | Vault Token Storage | 🔶 Blocked | Needs Vault integration |
| MEDIA-403 | Like Endpoint | 🔶 Blocked | Needs OAuth tokens |
| MEDIA-404 | Playlist Management | 🔶 Blocked | Needs OAuth tokens |
| MEDIA-625 | Primary Account OAuth Hardening | 🔶 Blocked | Needs OAuth + Vault |
| MEDIA-627 | OAuth Scope Minimization | 🔶 Blocked | Needs OAuth flow |
| MEDIA-628 | Emergency Token Revoke | ✅ Done | Kill switch endpoint, blocks all writes |
| MEDIA-629 | Vault Policy Hardening | 🔶 Blocked | Needs Vault setup |

## Epic: MEDIA-005 — CEC Remote Control

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-501 | CEC Event Listener | 🔶 Blocked | Needs Pi + HDMI-CEC |
| MEDIA-502 | Command Mapping | 🔶 Blocked | Needs MEDIA-501 |
| MEDIA-503 | Forward Commands to API | 🔶 Blocked | Needs MEDIA-502 |

## Epic: MEDIA-006 — Web UI

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-611 | TV On-Screen Overlay | ✅ Done | `/tv.html` with SSE |
| MEDIA-617 | Playback Policy Engine | 🔶 Blocked | Needs MEDIA-604 roles |
| MEDIA-632 | Device & Session Management | 🔶 Blocked | Needs Keycloak |

## Epic: MEDIA-007 — Queue Logic

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-619 | Redis Schema Documentation | ✅ Done | `docs/REDIS-SCHEMA.md` |

## Epic: MEDIA-008 — Multi-Device Playback

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-615 | Multi-Room Playback | 🔶 Blocked | Needs Pi nodes |
| MEDIA-616 | Offline Fallback Content | 🔶 Blocked | Needs MEDIA-612 cache |

## Infrastructure & Security

| Story | Title | Status | Notes |
|-------|-------|--------|-------|
| MEDIA-603 | Health Checks | ✅ Done | `/health/live`, `/health/ready` |
| MEDIA-606 | Redis Persistence | ✅ Done | AOF + RDB, custom redis.conf |
| MEDIA-623 | Architecture Enforcement | ✅ Done | 8 reflection-based boundary tests |
| MEDIA-604 | AuthZ & Rate Limiting | ✅ Done | JWT auth ready, fixed-window rate limits, audit log |
| MEDIA-605 | Vault Secret Storage | 🔶 Blocked | Needs Vault |
| MEDIA-621 | Internal Network Restrictions | ✅ Done | Audit middleware, request logging, anomaly detection |
| MEDIA-622 | Trusted Worker Communication | 🔶 Blocked | Needs MEDIA-621 |
| MEDIA-626 | Keycloak Gate for YouTube | 🔶 Blocked | Needs Keycloak + OAuth |

---

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/queue` | List queue items |
| POST | `/queue/add` | Add item to queue |
| DELETE | `/queue/{id}` | Remove queue item |
| GET | `/queue/mode` | Get queue mode |
| POST | `/queue/mode` | Set queue mode (Normal/Shuffle/PlayNext) |
| POST | `/player/play` | Play |
| POST | `/player/pause` | Pause |
| POST | `/player/skip` | Skip to next |
| POST | `/player/stop` | Stop playback |
| POST | `/player/position` | Report playback position |
| POST | `/player/error` | Report playback error |
| GET | `/now-playing` | Get current playback state |
| GET | `/events` | SSE event stream |
| GET | `/webhooks` | List registered webhooks |
| POST | `/webhooks` | Register webhook |
| DELETE | `/webhooks/{id}` | Remove webhook |
| GET | `/analytics` | Get analytics snapshot |
| GET | `/analytics/export` | Export analytics as JSON |
| GET | `/health` | Basic health |
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe (Redis) |
| GET | `/index.html` | Control deck UI |
| GET | `/tv.html` | TV overlay UI |

## Next Steps to Unblock

To unlock the remaining 28 stories, set up in this order:

1. **Keycloak** — Enables auth stories (604, 610, 617, 621–622, 626, 630–632)
2. **Vault + VaultFacade** — Enables secret stories (605, 629)
3. **Google OAuth** — Enables YouTube stories (401–404, 625, 627–628)
4. **Raspberry Pi** — Enables player node stories (201–205, 501–503, 612, 615–616)
