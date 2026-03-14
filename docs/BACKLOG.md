# Media Platform — Backlog

> Last updated: 2026-03-16

## Legend

| Status    | Meaning |
|-----------|---------|
| ✅ Done   | Implemented, tested, merged |
| 🔶 Blocked | Requires external service (Keycloak, Vault, Google OAuth, Raspberry Pi) |
| ⏳ Planned | Can be implemented independently |

## Summary

- **23 stories done** (all tested, committed)
- **123 tests** (93 unit + 30 integration), 0 failures
- **22 stories blocked** by external dependencies
- **21 new frontend/integration stories** planned across 4 epics

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
| MEDIA-617 | Playback Policy Engine | ✅ Done | BlockedChannel, TimeWindow, BlockedUrlPattern, MaxQueueSize, MaxDuration |
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
| MEDIA-622 | Trusted Worker Communication | ✅ Done | X-Worker-Key header auth, audit logging |
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
| GET | `/policies` | List playback policies |
| POST | `/policies` | Add playback policy |
| DELETE | `/policies/{id}` | Remove policy |
| POST | `/policies/{id}/toggle` | Enable/disable policy |
| POST | `/policies/evaluate` | Dry-run policy evaluation |
| POST | `/admin/kill-switch` | Toggle emergency kill switch |
| GET | `/admin/kill-switch` | Get kill switch status |
| GET | `/admin/audit` | Get audit log entries |
| GET | `/admin/anomalies` | Check for anomalies |

## Next Steps to Unblock

To unlock the remaining 22 blocked stories, set up in this order:

1. **Keycloak** — Enables auth stories (610, 626, 630, 632)
2. **Vault + VaultFacade** — Enables secret stories (605, 629)
3. **Google OAuth** — Enables YouTube stories (401–404, 625, 627)
4. **Raspberry Pi** — Enables player node stories (201–205, 501–503, 612, 615–616)

---

## Epic: MEDIA-FE-ADMIN — Admin & User Frontend (Vue 3 SPA)

> Mobile-first PWA for queue management, search, and admin controls.  
> Tech stack: Vue 3 + Vite + TailwindCSS v4 + shadcn-vue + Pinia + TanStack Query + Orval + keycloak-js

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-700 | Admin Frontend Project Setup | 3 pts | ⏳ Planned | — |
| MEDIA-701 | Keycloak Auth Flow | 3 pts | ⏳ Planned | MEDIA-700 |
| MEDIA-702 | Queue Management View | 3 pts | ⏳ Planned | MEDIA-700, MEDIA-701, MEDIA-704 |
| MEDIA-703 | Admin Dashboard View | 3 pts | ⏳ Planned | MEDIA-700, MEDIA-701 |
| MEDIA-704 | SSE Real-Time Composable & Player Store | 3 pts | ⏳ Planned | MEDIA-700 |
| MEDIA-705 | PWA Configuration | 2 pts | ⏳ Planned | MEDIA-700 |
| MEDIA-706 | Frontend Docker & Deployment | 2 pts | ⏳ Planned | MEDIA-700 |
| MEDIA-707 | Mobile Navigation & App Layout | 2 pts | ⏳ Planned | MEDIA-700, MEDIA-701 |
| MEDIA-708 | Orval Client Generation Pipeline | 2 pts | ⏳ Planned | MEDIA-700, MEDIA-712 |
| MEDIA-709 | Error Handling & Toast Notification System | 2 pts | ⏳ Planned | MEDIA-700 |
| MEDIA-710 | YouTube Search Integration (Invidious) | 3 pts | ⏳ Planned | MEDIA-700, MEDIA-701 |
| MEDIA-714 | Loading States & Skeleton Screens | 2 pts | ⏳ Planned | MEDIA-700 |
| MEDIA-715 | E2E Testing with Playwright | 3 pts | ⏳ Planned | MEDIA-700, MEDIA-701, MEDIA-702 |
| MEDIA-716 | Invidious Search Resilience & Failover | 2 pts | ⏳ Planned | MEDIA-710 |

## Epic: MEDIA-FE-TV — TV Frontend (Pi Kiosk)

> Lightweight HTML/JS application for Raspberry Pi Chromium kiosk mode.  
> Fullscreen YouTube player with CEC remote control and on-screen search.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-720 | TV Frontend — Pi Kiosk Application | 5 pts | ⏳ Planned | MEDIA-704 (pattern) |
| MEDIA-721 | CEC Remote Control Integration | 3 pts | ⏳ Planned | MEDIA-720 |
| MEDIA-722 | TV On-Screen Keyboard & Search | 3 pts | ⏳ Planned | MEDIA-720, MEDIA-721, MEDIA-710 |
| MEDIA-723 | Pi Provisioning & Setup Automation | 3 pts | ⏳ Planned | MEDIA-720, MEDIA-721 |

## Epic: MEDIA-BE-MULTI — Multi-User Backend Support

> API extensions for user tracking and OpenAPI spec generation.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-711 | Track "Added By" User on Queue Items | 2 pts | ⏳ Planned | MEDIA-604 (JWT) |
| MEDIA-712 | OpenAPI Spec Generation | 1 pt | ⏳ Planned | — |
| MEDIA-713 | Guest Access Model (TV Anonymous + SPA Auth) | 3 pts | ⏳ Planned | MEDIA-604, MEDIA-622, MEDIA-711 |

## Epic: MEDIA-MULTI — Multi-Device Audio (v2)

> Personal playback sessions — users listen to different songs on their devices.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-730 | Multi-Device Audio — Separate Playback Sessions | 8 pts | ⏳ Planned | MEDIA-711, MEDIA-701 |

---

## Recommended Build Order

### Phase 1: Foundation (Backend + Scaffold)
1. **MEDIA-712** — OpenAPI spec generation
2. **MEDIA-711** — Track added-by user (backend)
3. **MEDIA-713** — Guest access model + CORS (backend)
4. **MEDIA-700** — Frontend project setup (scaffold + tooling)
5. **MEDIA-708** — Orval client generation pipeline

### Phase 2: Core SPA
6. **MEDIA-709** — Error handling & toast system
7. **MEDIA-714** — Loading states & skeleton screens
8. **MEDIA-701** — Keycloak auth flow
9. **MEDIA-704** — SSE composable + player store
10. **MEDIA-707** — Nav + layout shell
11. **MEDIA-702** — Queue management view

### Phase 3: Features
12. **MEDIA-710** — YouTube search (Invidious)
13. **MEDIA-716** — Invidious search resilience & failover
14. **MEDIA-703** — Admin dashboard
15. **MEDIA-705** — PWA config
16. **MEDIA-706** — Docker deployment

### Phase 4: TV Experience
17. **MEDIA-720** — TV kiosk app
18. **MEDIA-721** — CEC remote
19. **MEDIA-722** — TV on-screen keyboard
20. **MEDIA-723** — Pi provisioning script

### Phase 5: Quality & Testing
21. **MEDIA-715** — E2E testing with Playwright

### Phase 6: Multi-Device (v2)
22. **MEDIA-730** — Personal playback sessions
