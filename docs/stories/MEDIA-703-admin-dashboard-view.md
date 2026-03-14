# MEDIA-703: Admin Dashboard View

## Story

**Epic:** MEDIA-FE-ADMIN — Admin & User Frontend  
**Priority:** High  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700, MEDIA-701

---

## Summary

Admin-only dashboard accessible at `/admin`. Provides security controls, analytics overview, audit log, and policy management. Only visible to users with `media-admin` role.

---

## UI Layout

```
┌──────────────────────────────────────┐
│  🛡️ Admin Dashboard                  │
├──────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────┐  │
│  │ Status  │  │Commands │  │Errors│  │
│  │ ● LIVE  │  │  1,247  │  │  3   │  │
│  └─────────┘  └─────────┘  └─────┘  │
├──────────────────────────────────────┤
│  🚨 Kill Switch          [ACTIVATE]  │
│  Status: Operational                  │
├──────────────────────────────────────┤
│  📋 Policies (2 active)              │
│  ┌────────────────────────────────┐  │
│  │ Block Rickrolls  ✅  [Toggle]  │  │
│  │ Daytime Only     ✅  [Toggle]  │  │
│  │            [+ Add Policy]      │  │
│  └────────────────────────────────┘  │
├──────────────────────────────────────┤
│  📊 Recent Audit Log                │
│  12:05 POST /queue/add → 201        │
│  12:04 POLICY_DENIED → rickroll      │
│  12:03 POST /player/skip → 200      │
│      [View Full Log]                 │
├──────────────────────────────────────┤
│  🔍 Anomaly Detection               │
│  No anomalies detected ✅            │
└──────────────────────────────────────┘
```

---

## Features

- **Status cards** — Live system metrics (total commands, errors, avg latency, playback time)
- **Kill switch** — Activate/deactivate with reason prompt
- **Policy management** — List, add, toggle, remove playback policies
- **Audit log** — Recent entries with filtering (action type, time range)
- **Anomaly detection** — Current anomaly status with alert details

---

## Components

| Component             | Purpose                    |
| --------------------- | -------------------------- |
| `AdminView.vue`       | Main admin layout          |
| `StatusCards.vue`     | Analytics metrics as cards |
| `KillSwitchPanel.vue` | Kill switch controls       |
| `PolicyManager.vue`   | Policy CRUD                |
| `PolicyForm.vue`      | Add/edit policy dialog     |
| `AuditLog.vue`        | Scrollable audit entries   |
| `AnomalyPanel.vue`    | Anomaly detection status   |

---

## API Endpoints Used

| Method | Path                    | Purpose                |
| ------ | ----------------------- | ---------------------- |
| GET    | `/analytics`            | System metrics         |
| POST   | `/admin/kill-switch`    | Activate kill switch   |
| DELETE | `/admin/kill-switch`    | Deactivate kill switch |
| GET    | `/admin/kill-switch`    | Kill switch status     |
| GET    | `/admin/audit`          | Audit log entries      |
| GET    | `/admin/anomalies`      | Anomaly report         |
| GET    | `/policies`             | List policies          |
| POST   | `/policies`             | Add policy             |
| DELETE | `/policies/{id}`        | Remove policy          |
| POST   | `/policies/{id}/toggle` | Toggle policy          |
| POST   | `/policies/evaluate`    | Dry-run test           |

---

## Tasks

- [ ] Create `/admin` route with `media-admin` role guard
- [ ] Create `AdminView.vue` with card-based layout
- [ ] Create `StatusCards.vue` with TanStack Query polling (15s)
- [ ] Create `KillSwitchPanel.vue` with activate/deactivate + reason dialog
- [ ] Create `PolicyManager.vue` with list + toggle + delete
- [ ] Create `PolicyForm.vue` dialog for adding new policies (type selector, value input)
- [ ] Create `AuditLog.vue` with virtualized scroll for large lists
- [ ] Create `AnomalyPanel.vue` with status indicator
- [ ] Style with shadcn-vue Card, Badge, Button, Dialog, Table

---

## Acceptance Criteria

- [ ] Only accessible to `media-admin` role users
- [ ] Kill switch activate/deactivate works with confirmation dialog
- [ ] Policies can be added, toggled, and removed
- [ ] Audit log displays recent entries with action and timestamp
- [ ] Analytics cards show live metrics
- [ ] Anomaly status is visible
- [ ] Responsive on mobile (stacked cards)
