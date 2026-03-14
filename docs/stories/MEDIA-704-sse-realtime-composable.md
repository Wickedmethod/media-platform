# MEDIA-704: SSE Real-Time Composable & Player Store

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** High  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700 (project setup)

---

## Summary

Create a `useSSE` composable that connects to the API's `/events` SSE endpoint and a Pinia `usePlayerStore` that maintains real-time playback state. All components across the app reactively consume this store — no individual polling.

---

## Architecture

```
API /events (SSE)
    │
    ▼
useSSE composable (EventSource + reconnect)
    │
    ▼
usePlayerStore (Pinia)
    ├── currentItem: QueueItem | null
    ├── playerState: 'Playing' | 'Paused' | 'Stopped' | 'Idle'
    ├── queueMode: 'Sequential' | 'Shuffle' | 'RepeatOne' | 'RepeatAll'
    ├── position: number (seconds)
    ├── isKillSwitchActive: boolean
    └── lastUpdate: Date
    │
    ▼
Vue Components (reactive via storeToRefs)
```

---

## SSE Composable: `useSSE.ts`

```typescript
// src/composables/useSSE.ts
export function useSSE(url: string, options?: UseSSEOptions) {
  const isConnected = ref(false)
  const lastEvent = ref<SSEEvent | null>(null)
  const error = ref<string | null>(null)
  let eventSource: EventSource | null = null
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null

  function connect() {
    eventSource = new EventSource(url)

    eventSource.onopen = () => {
      isConnected.value = true
      error.value = null
    }

    eventSource.onmessage = (event) => {
      lastEvent.value = JSON.parse(event.data)
    }

    eventSource.onerror = () => {
      isConnected.value = false
      eventSource?.close()
      // Exponential backoff: 1s, 2s, 4s, 8s, max 30s
      scheduleReconnect()
    }
  }

  function disconnect() {
    eventSource?.close()
    eventSource = null
    isConnected.value = false
    if (reconnectTimer) clearTimeout(reconnectTimer)
  }

  // Auto-connect on mount, disconnect on unmount
  onMounted(connect)
  onUnmounted(disconnect)

  return { isConnected, lastEvent, error, connect, disconnect }
}
```

### Features

- **Automatic reconnect** with exponential backoff (1s → 30s max)
- **Connection state tracking** — show "Reconnecting..." in UI
- **Auth token injection** — if SSE endpoint requires JWT, fall back to polling with `fetch` + `Authorization` header (EventSource doesn't support custom headers)
- **Heartbeat detection** — if no event in 60s, assume dead connection and reconnect

---

## Player Store: `usePlayerStore.ts`

```typescript
// src/stores/usePlayerStore.ts
export const usePlayerStore = defineStore('player', () => {
  // State
  const currentItem = ref<QueueItem | null>(null)
  const playerState = ref<PlayerState>('Idle')
  const queueMode = ref<QueueMode>('Sequential')
  const position = ref(0)
  const duration = ref(0)
  const isKillSwitchActive = ref(false)
  const lastUpdate = ref<Date>(new Date())

  // Computed
  const isPlaying = computed(() => playerState.value === 'Playing')
  const isPaused = computed(() => playerState.value === 'Paused')
  const progress = computed(() =>
    duration.value > 0 ? (position.value / duration.value) * 100 : 0
  )

  // Actions — called by SSE handler
  function handleSSEEvent(event: SSEEvent) {
    switch (event.type) {
      case 'state-changed':
        playerState.value = event.data.state
        break
      case 'track-changed':
        currentItem.value = event.data.item
        position.value = 0
        break
      case 'position-updated':
        position.value = event.data.position
        duration.value = event.data.duration
        break
      case 'queue-updated':
        // Invalidate TanStack Query cache for queue
        queryClient.invalidateQueries({ queryKey: ['queue'] })
        break
      case 'kill-switch':
        isKillSwitchActive.value = event.data.active
        break
    }
    lastUpdate.value = new Date()
  }

  return {
    currentItem, playerState, queueMode, position, duration,
    isKillSwitchActive, lastUpdate,
    isPlaying, isPaused, progress,
    handleSSEEvent,
  }
})
```

---

## SSE ↔ TanStack Query Integration

The SSE composable does **not** replace TanStack Query. Instead:

| Data | Source | Reason |
|------|--------|--------|
| Player state | SSE → Pinia store | Real-time, push |
| Queue list | TanStack Query | Paginated, cached |
| Analytics | TanStack Query | Polled, admin-only |
| Policies | TanStack Query | Rarely changes |
| Audit log | TanStack Query | On-demand fetch |

When SSE receives `queue-updated`, it **invalidates** the TanStack Query cache:

```typescript
const queryClient = useQueryClient()
queryClient.invalidateQueries({ queryKey: ['queue'] })
```

This triggers a refetch only if the component is currently mounted and watching that query.

---

## Polling Fallback

If SSE fails to connect after 3 retries, fall back to polling:

```typescript
const POLL_INTERVAL = 5_000 // 5 seconds

function startPolling() {
  pollTimer = setInterval(async () => {
    const state = await api.getNowPlaying()
    handleSSEEvent({ type: 'state-changed', data: state })
  }, POLL_INTERVAL)
}
```

---

## Connection Status UI

A small indicator in the app shell shows connection status:

```
● Connected (green dot)
◌ Reconnecting... (amber, pulsing)
✕ Offline (red, with "Retry" button)
```

---

## Tasks

- [ ] Create `src/composables/useSSE.ts` with EventSource wrapper
- [ ] Implement exponential backoff reconnect logic
- [ ] Create `src/stores/usePlayerStore.ts` Pinia store
- [ ] Wire SSE events to store actions
- [ ] Add TanStack Query cache invalidation on `queue-updated`
- [ ] Implement polling fallback after 3 SSE failures
- [ ] Create connection status indicator component
- [ ] Add heartbeat detection (60s timeout)
- [ ] Write unit tests for store event handling
- [ ] Write unit tests for SSE reconnect behavior

---

## Acceptance Criteria

- [ ] SSE connects on app mount and pushes events to Pinia store
- [ ] Player state updates reactively in all components
- [ ] SSE auto-reconnects with exponential backoff on failure
- [ ] Falls back to polling if SSE is unavailable
- [ ] TanStack Query cache invalidated on queue update events
- [ ] Connection status indicator visible in app shell
- [ ] No memory leaks — EventSource closed on unmount
