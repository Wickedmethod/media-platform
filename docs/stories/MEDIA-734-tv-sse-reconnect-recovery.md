# MEDIA-734: TV SSE Reconnect & State Recovery

## Story

**Epic:** MEDIA-FE-TV — TV Frontend  
**Priority:** High  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-720 (TV frontend), MEDIA-704 (SSE composable pattern), MEDIA-725 (sync endpoint)

---

## Summary

Implement robust SSE reconnection for the TV Vue app with exponential backoff, heartbeat detection, and full state recovery via `/sync` on reconnect. The TV must never show stale data — when it reconnects, it fetches fresh state before resuming.

---

## Current Problem

The existing `tv.html` has a simple 3-second reconnect:
```javascript
eventSource.onerror = () => { setTimeout(connectSSE, 3000) }
```

This fails when:
- Network is down for extended periods (floods reconnect attempts)
- Server restarts (loses event history, client gets stale)
- Connection silently dies (no error event, just stops receiving)

---

## Reconnect Strategy

The TV Vue app reuses `useSSE` from the shared composables (MEDIA-704) with TV-specific recovery:

```typescript
// src/features/tv/useTvSSE.ts
import { useSSE } from '@/composables/useSSE'
import { usePlayerStore } from '@/stores/player'

export function useTvSSE() {
  const playerStore = usePlayerStore()
  const { isConnected, lastEvent, connect, disconnect } = useSSE('/events', {
    reconnect: {
      baseDelay: 1000,
      maxDelay: 30000,
      maxRetries: Infinity, // TV never gives up
    },
    heartbeatTimeout: 60_000, // Reconnect if no event in 60s
    onReconnect: async () => {
      // Fetch full state on every reconnect
      const snapshot = await fetch('/sync').then(r => r.json())
      playerStore.applySnapshot(snapshot)
    },
  })

  return { isConnected, lastEvent, connect, disconnect }
}
```

---

## Heartbeat Detection

Server sends `heartbeat` events every 30s (MEDIA-712). If no event arrives in 60s, the connection is assumed dead:

```typescript
let heartbeatTimer: ReturnType<typeof setTimeout>

function resetHeartbeatTimer() {
  clearTimeout(heartbeatTimer)
  heartbeatTimer = setTimeout(() => {
    // No event in 60s — connection is dead
    eventSource.close()
    scheduleReconnect()
  }, 60_000)
}

// Reset on every event (including heartbeat)
eventSource.onmessage = (event) => {
  resetHeartbeatTimer()
  handleEvent(JSON.parse(event.data))
}
```

---

## State Recovery on Reconnect

After reconnecting, the TV fetches `/sync` and applies the snapshot:

| Server State | TV Action |
|-------------|-----------|
| Playing (same video) | Seek to server position |
| Playing (different video) | Load new video at server position |
| Paused | Load video, pause at position |
| Idle | Show idle screen |
| Kill switch active | Show blocked screen |

---

## Connection Status Indicator

Subtle indicator in bottom-right corner of TV:

```
● Connected           (green, fades after 5s)
◌ Reconnecting...     (amber, pulsing, persistent)
✕ Offline             (red, after 10 failed retries, persistent)
```

---

## Tasks

- [ ] Create `useTvSSE` composable wrapping shared `useSSE`
- [ ] Implement `onReconnect` callback with `/sync` fetch
- [ ] Implement `applySnapshot` in player store
- [ ] Add heartbeat timeout detection (60s no-event)
- [ ] Add connection status indicator to TV overlay
- [ ] Test: disconnect network → verify backoff → reconnect → verify state recovery
- [ ] Test: kill API → restart → verify TV recovers within 30s

---

## Acceptance Criteria

- [ ] TV reconnects with exponential backoff after disconnect
- [ ] State is fully recovered via `/sync` on every reconnect
- [ ] Silent connection death detected via heartbeat timeout
- [ ] Connection status visible on TV screen
- [ ] TV never shows stale data after reconnect
