# MEDIA-732: Player Log Streaming & Remote Diagnostics

## Story

**Epic:** MEDIA-002 — Raspberry Pi Player Node  
**Priority:** Low  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-729 (registration), MEDIA-724 (heartbeat)

---

## Summary

Enable Pi players to stream logs to the API for remote debugging. When a player misbehaves, admins can view its recent logs from the dashboard without SSH access. Logs are batched and sent periodically.

---

## Architecture

```
Pi Player (TV Vue app + CEC bridge)
    │ console.log, console.error captured
    ▼
Log buffer (in-memory, max 500 entries)
    │ Flush every 30s or on error
    ▼
POST /diagnostics/logs
X-Worker-Key: <key>
    │
    ▼
API stores in Redis (ring buffer, last 1000 entries per player)
    │
    ▼
GET /admin/players/{id}/logs  (admin view)
```

---

## Log Collection on Pi

```typescript
// src/composables/useLogCollector.ts
const logBuffer: LogEntry[] = [];

interface LogEntry {
  timestamp: string;
  level: "debug" | "info" | "warn" | "error";
  message: string;
  source: "tv" | "cec" | "sse" | "player";
}

// Intercept console methods
const originalLog = console.log;
console.log = (...args) => {
  logBuffer.push({
    timestamp: new Date().toISOString(),
    level: "info",
    message: args.join(" "),
    source: "tv",
  });
  originalLog.apply(console, args);
};

// Flush periodically
setInterval(flushLogs, 30_000);

async function flushLogs() {
  if (logBuffer.length === 0) return;
  const batch = logBuffer.splice(0, logBuffer.length);
  await fetch("/diagnostics/logs", {
    method: "POST",
    headers: { "X-Worker-Key": workerKey, "Content-Type": "application/json" },
    body: JSON.stringify({ playerId, entries: batch }),
  }).catch(() => {
    // On failure, put entries back (up to max buffer)
    logBuffer.unshift(...batch.slice(-200));
  });
}
```

---

## Endpoints

### Submit Logs

```
POST /diagnostics/logs
X-Worker-Key: <key>

{
  "playerId": "living-room-tv",
  "entries": [
    { "timestamp": "2026-03-16T14:32:00Z", "level": "error", "message": "YouTube player error code 150", "source": "player" },
    { "timestamp": "2026-03-16T14:32:01Z", "level": "info", "message": "Reporting error to API", "source": "tv" }
  ]
}

Response: 204 No Content
```

### View Logs (admin)

```
GET /admin/players/{id}/logs?level=error&limit=100
Authorization: Bearer <admin-jwt>

Response:
{
  "playerId": "living-room-tv",
  "entries": [ ... ],
  "totalCount": 342
}
```

---

## Tasks

- [ ] Create `POST /diagnostics/logs` endpoint (WorkerOnly)
- [ ] Store logs in Redis list (ring buffer, max 1000 per player)
- [ ] Create `GET /admin/players/{id}/logs` (AdminOnly, with level filter)
- [ ] Implement log collector composable for TV app
- [ ] Add immediate flush on `console.error` (don't wait for timer)
- [ ] Integration test for log submission + retrieval

---

## Acceptance Criteria

- [ ] Player logs arrive at API within 30s
- [ ] Errors flush immediately
- [ ] Admin can filter logs by level
- [ ] Oldest logs evicted when buffer exceeds 1000 entries
