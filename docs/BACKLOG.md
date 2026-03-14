# Media Platform — Backlog

> Last updated: 2026-03-17

## Legend

| Status    | Meaning |
|-----------|---------|----|
| ✅ Done   | Implemented, tested, merged |
| 🔶 Blocked | Requires external service (Keycloak, Vault, Google OAuth, Raspberry Pi) |
| ⏳ Planned | Can be implemented independently |

## Summary

- **48 stories done** (all tested, committed)
- **273 tests** (194 unit + 79 integration), 0 failures
- **22 stories blocked** by external dependencies
- **44 new frontend/integration/infra stories** planned across 9 epics

### Merge Log

The following proposed stories were merged into existing stories to avoid duplication:

| Merged | Into | Reason |
|--------|------|--------|
| MEDIA-717 (SSE Event Contract) | MEDIA-712 | SSE event types belong with OpenAPI spec |
| MEDIA-726 (Player Capabilities) | MEDIA-729 | Capabilities are part of registration handshake |
| MEDIA-727 (SSE Auth & Guest Policy) | MEDIA-713 | SSE auth is part of guest access model |
| MEDIA-735 (TV Idle Screen) | MEDIA-720 | Idle screen is a feature of the TV kiosk app |
| MEDIA-738 (Now Playing Panel) | MEDIA-702 | Player controls already in queue view |
| MEDIA-750 (Queue Empty State UI) | MEDIA-714 | Empty states already scoped in loading/skeleton story |
| MEDIA-753 (TV Idle Mode & Screensaver) | MEDIA-720 | TvIdle.vue already defined in TV kiosk story |
| MEDIA-754 (TV Player Overlay UI) | MEDIA-720 | TvOverlay.vue already defined in TV kiosk story |
| MEDIA-757 (Structured JSON Logging) | MEDIA-741 | Structured JSON logging fully covered in correlation IDs story |
| MEDIA-761 (Global Error Boundary & Fallback UI) | MEDIA-709 | Error boundary already scoped in task #8; expanded with ErrorBoundary.vue + FallbackError.vue |
| MEDIA-764 (Secrets Management for Docker Compose) | MEDIA-745 | .vault-env secret injection already scoped in environment configuration story |

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
| POST | `/player/heartbeat` | Player heartbeat (liveness) |
| POST | `/worker/register` | Player registration handshake |
| GET | `/admin/players` | List registered players |
| GET | `/admin/players/{id}/logs` | Get player diagnostic logs |
| GET | `/admin/players/versions` | Get player version matrix |
| POST | `/admin/players/expected-version` | Set expected player version |
| POST | `/admin/players/notify-update` | Notify players of update |
| POST | `/worker/disconnect` | Graceful player disconnect |
| GET | `/admin/players/{id}/network` | Get player network metrics + trend |
| GET | `/admin/alerts/config` | Get alerting configuration status |
| GET | `/sync` | Queue snapshot (atomic state) |
| POST | `/queue/reorder` | Reorder queue items (admin) |
| POST | `/diagnostics/logs` | Submit player log batch |
| POST | `/diagnostics/network` | Submit network connectivity metrics |
| GET | `/diagnostics/bandwidth-test` | Bandwidth test payload (100 KB) |
| GET | `/metrics` | Prometheus metrics |
| POST | `/queue/validate` | Validate queue item (dry-run) |

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
| MEDIA-700 | Admin Frontend Project Setup | 3 pts | ✅ Done | — |
| MEDIA-701 | Keycloak Auth Flow | 3 pts | ✅ Done | MEDIA-700 |
| MEDIA-702 | Queue Management View & Now Playing Panel | 3 pts | ✅ Done | MEDIA-700, MEDIA-701, MEDIA-704 |
| MEDIA-703 | Admin Dashboard View | 3 pts | ✅ Done | MEDIA-700, MEDIA-701 |
| MEDIA-704 | SSE Real-Time Composable & Player Store | 3 pts | ✅ Done | MEDIA-700 |
| MEDIA-705 | PWA Configuration | 2 pts | ✅ Done | MEDIA-700 |
| MEDIA-706 | Frontend Docker & Deployment | 2 pts | ✅ Done | MEDIA-700 |
| MEDIA-707 | Mobile Navigation & App Layout | 2 pts | ✅ Done | MEDIA-700, MEDIA-701 |
| MEDIA-708 | Orval Client Generation Pipeline | 2 pts | ✅ Done | MEDIA-700, MEDIA-712 |
| MEDIA-709 | Error Handling & Toast Notification System | 2 pts | ✅ Done | MEDIA-700 |
| MEDIA-710 | YouTube Search Integration (Invidious) | 3 pts | ✅ Done | MEDIA-700, MEDIA-701 |
| MEDIA-714 | Loading States & Skeleton Screens | 2 pts | ✅ Done | MEDIA-700 |
| MEDIA-715 | E2E Testing with Playwright | 3 pts | ⏳ Planned | MEDIA-700, MEDIA-701, MEDIA-702 |
| MEDIA-716 | Invidious Search Resilience & Failover | 2 pts | ⏳ Planned | MEDIA-710 |
| MEDIA-737 | Queue Reordering — Drag & Drop | 3 pts | ⏳ Planned | MEDIA-702 |
| MEDIA-739 | Queue Item Details Modal | 2 pts | ⏳ Planned | MEDIA-702 |
| MEDIA-740 | User Activity Indicators ("Added By") | 2 pts | ⏳ Planned | MEDIA-711 |
| MEDIA-748 | Player Command Rate Limiting & Debounce | 2 pts | ✅ Done | MEDIA-704, MEDIA-702 |
| MEDIA-751 | Global Connection Status Indicator | 2 pts | ✅ Done | MEDIA-704, MEDIA-707 |
| MEDIA-752 | Reconnect & Offline Banner for SPA | 2 pts | ⏳ Planned | MEDIA-704, MEDIA-751 |
| MEDIA-762 | Feature Flag System for Frontend | 2 pts | ✅ Done | MEDIA-700 |

## Epic: MEDIA-FE-TV — TV Frontend (Pi Kiosk)

> Vue 3 second entry point sharing composables with the SPA.  
> Fullscreen YouTube player on Raspberry Pi Chromium kiosk with CEC remote and on-screen search.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-720 | TV Frontend — Pi Kiosk Application (Vue) | 5 pts | ⏳ Planned | MEDIA-700, MEDIA-704 |
| MEDIA-721 | CEC Remote Control Integration | 3 pts | ⏳ Planned | MEDIA-720 |
| MEDIA-722 | TV On-Screen Keyboard & Search (Vue) | 3 pts | ⏳ Planned | MEDIA-720, MEDIA-721, MEDIA-710 |
| MEDIA-723 | Pi Provisioning & Setup Automation | 3 pts | ⏳ Planned | MEDIA-720, MEDIA-721 |
| MEDIA-734 | TV SSE Reconnect & State Recovery | 2 pts | ⏳ Planned | MEDIA-720, MEDIA-704 |
| MEDIA-736 | TV Playback Error Screen | 2 pts | ⏳ Planned | MEDIA-720 |

## Epic: MEDIA-BE-MULTI — Multi-User Backend Support

> API extensions for user tracking and OpenAPI spec generation.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-711 | Track "Added By" User on Queue Items | 2 pts | ✅ Done | MEDIA-604 (JWT) |
| MEDIA-712 | OpenAPI Spec Generation & SSE Event Contract | 2 pts | ✅ Done | — |
| MEDIA-713 | Guest Access Model & SSE Authorization | 3 pts | ✅ Done | MEDIA-604, MEDIA-622, MEDIA-711 |
| MEDIA-747 | Queue Item Metadata Enrichment (YouTube Fetch) | 3 pts | ⏳ Planned | MEDIA-710 |
| MEDIA-749 | Queue Item Validation & Sanitization | 2 pts | ✅ Done | — |
| MEDIA-759 | API Versioning Strategy | 3 pts | ⏳ Planned | MEDIA-712 |

## Epic: MEDIA-BE-RESILIENCE — Backend Resilience & Consistency

> Race condition protection, client-server sync, and queue consistency guarantees.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-724 | Player Heartbeat & Liveness Reporting | 3 pts | ✅ Done | MEDIA-622 |
| MEDIA-725 | Queue Snapshot Endpoint for Fast Client Sync | 2 pts | ✅ Done | — |
| MEDIA-728 | Queue Consistency Guard (Race Condition Protection) | 3 pts | ✅ Done | — |
| MEDIA-729 | Player Registration & Capability Handshake | 3 pts | ✅ Done | MEDIA-622, MEDIA-724 |

## Epic: MEDIA-PI-OPS — Player Operations & Diagnostics

> Remote monitoring, diagnostics, and lifecycle management for Raspberry Pi player nodes.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-731 | Player Crash Recovery & Auto-Reconnect | 3 pts | ⏳ Planned | MEDIA-725, MEDIA-734 |
| MEDIA-732 | Player Log Streaming & Remote Diagnostics | 2 pts | ✅ Done | MEDIA-729 |
| MEDIA-733 | Player Version & Update Check | 2 pts | ✅ Done | MEDIA-729 |
| MEDIA-755 | Player Playback Timeout Detection | 2 pts | ⏳ Planned | MEDIA-720, MEDIA-724 |
| MEDIA-756 | Player Disk Usage & Cache Cleanup | 2 pts | ⏳ Planned | MEDIA-612, MEDIA-729 |
| MEDIA-760 | Graceful Player Shutdown Handling | 2 pts | ✅ Done | MEDIA-724, MEDIA-729 |
| MEDIA-763 | Player Network Connectivity Monitoring | 3 pts | ✅ Done | MEDIA-729, MEDIA-732 |

## Epic: MEDIA-OBS — Infrastructure & Observability

> Logging, metrics, and alerting for production monitoring.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-741 | Centralized Logging & Correlation IDs | 3 pts | ✅ Done | — |
| MEDIA-742 | Metrics Export — Prometheus Format | 2 pts | ✅ Done | — |
| MEDIA-743 | Alerting Integration for Anomalies | 2 pts | ✅ Done | MEDIA-631, MEDIA-613 |
| MEDIA-758 | Backup & Restore Strategy for Redis | 2 pts | ✅ Done | MEDIA-606 |

## Epic: MEDIA-DEPLOY — Deployment & Configuration

> Docker Compose stack, environment config, and production reverse proxy.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-744 | Docker Compose Stack for Full Platform | 3 pts | ⏳ Planned | MEDIA-706 |
| MEDIA-745 | Environment Configuration Management | 2 pts | ⏳ Planned | MEDIA-744 |
| MEDIA-746 | Production Reverse Proxy Configuration | 2 pts | ⏳ Planned | MEDIA-744, MEDIA-706 |
| MEDIA-765 | Platform Upgrade & Migration Strategy | 3 pts | ⏳ Planned | MEDIA-744, MEDIA-606 |

## Epic: MEDIA-MULTI — Multi-Device Audio (v2)

> Personal playback sessions — users listen to different songs on their devices.

| Story | Title | Effort | Status | Depends on |
|-------|-------|--------|--------|------------|
| MEDIA-730 | Multi-Device Audio — Separate Playback Sessions | 8 pts | ⏳ Planned | MEDIA-711, MEDIA-701 |

---

## Recommended Build Order

### Phase 1: Foundation (Backend + Scaffold)
1. **MEDIA-712** — OpenAPI spec generation & SSE event contract
2. **MEDIA-749** — Queue item validation & sanitization
3. **MEDIA-711** — Track added-by user (backend)
4. **MEDIA-747** — Queue item metadata enrichment (YouTube fetch)
5. **MEDIA-713** — Guest access model & SSE authorization
6. **MEDIA-700** — Frontend project setup (scaffold + tooling + TV entry point)
7. **MEDIA-708** — Orval client generation pipeline

### Phase 2: Backend Resilience
8. **MEDIA-725** — Queue snapshot endpoint
9. **MEDIA-728** — Queue consistency guard
10. **MEDIA-724** — Player heartbeat & liveness
11. **MEDIA-729** — Player registration & capability handshake
12. **MEDIA-748** — Player command rate limiting & debounce

### Phase 3: Core SPA
13. **MEDIA-709** — Error handling & toast system
14. **MEDIA-714** — Loading states & skeleton screens (incl. empty states)
15. **MEDIA-701** — Keycloak auth flow
16. **MEDIA-704** — SSE composable + player store
17. **MEDIA-751** — Global connection status indicator
18. **MEDIA-752** — Reconnect & offline banner for SPA
19. **MEDIA-707** — Nav + layout shell
20. **MEDIA-702** — Queue management view & now playing panel

### Phase 4: SPA Features
21. **MEDIA-710** — YouTube search (Invidious)
22. **MEDIA-716** — Invidious search resilience & failover
23. **MEDIA-703** — Admin dashboard
24. **MEDIA-737** — Queue reordering (drag & drop)
25. **MEDIA-739** — Queue item details modal
26. **MEDIA-740** — User activity indicators
27. **MEDIA-705** — PWA config
28. **MEDIA-706** — Docker deployment

### Phase 5: TV Experience
29. **MEDIA-720** — TV kiosk app (Vue, incl. idle/overlay)
30. **MEDIA-721** — CEC remote
31. **MEDIA-722** — TV on-screen keyboard (Vue)
32. **MEDIA-734** — TV SSE reconnect & state recovery
33. **MEDIA-736** — TV playback error screen
34. **MEDIA-723** — Pi provisioning script

### Phase 6: Player Operations
35. **MEDIA-731** — Player crash recovery
36. **MEDIA-732** — Player log streaming
37. **MEDIA-733** — Player version & update check
38. **MEDIA-755** — Player playback timeout detection
39. **MEDIA-756** — Player disk usage & cache cleanup
40. **MEDIA-760** — Graceful player shutdown handling
41. **MEDIA-763** — Player network connectivity monitoring

### Phase 7: Observability & Infra
42. **MEDIA-741** — Centralized logging & correlation IDs (incl. JSON format)
43. **MEDIA-742** — Metrics export (Prometheus)
44. **MEDIA-743** — Alerting integration for anomalies
45. **MEDIA-758** — Backup & restore strategy for Redis

### Phase 8: Deployment
46. **MEDIA-744** — Docker Compose stack for full platform
47. **MEDIA-745** — Environment configuration management (incl. secrets)
48. **MEDIA-746** — Production reverse proxy
49. **MEDIA-765** — Platform upgrade & migration strategy

### Phase 9: API Maturity & Quality
50. **MEDIA-759** — API versioning strategy
51. **MEDIA-762** — Feature flag system for frontend
52. **MEDIA-715** — E2E testing with Playwright

### Phase 10: Multi-Device (v2)
53. **MEDIA-730** — Personal playback sessions
