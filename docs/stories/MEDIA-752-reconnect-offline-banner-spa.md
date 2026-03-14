# MEDIA-752: Reconnect & Offline Banner for SPA

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-704 (SSE composable), MEDIA-751 (connection status indicator)

---

## Summary

When the SPA loses connectivity (API down, network offline, SSE stream dropped), show a dismissible banner explaining the situation and offering manual retry. Unlike the TV (MEDIA-734) which auto-recovers silently, the SPA gives users explicit control and stale-data warnings.

**Related:** MEDIA-751 provides the visual indicator. This story adds the **offline behavior layer**: stale data marking, manual retry, and data freshness tracking.

---

## Offline Behavior

### 1. Stale Data Warning

When disconnected, existing data is still visible but marked as potentially stale:

```vue
<!-- src/shared/components/StaleDataOverlay.vue -->
<template>
  <div v-if="!isConnected" class="relative">
    <div class="absolute inset-0 bg-background/50 backdrop-blur-[1px] z-10
                flex items-center justify-center pointer-events-none">
      <Badge variant="outline" class="bg-background pointer-events-auto">
        <WifiOff class="h-3 w-3 mr-1" /> Data may be outdated
      </Badge>
    </div>
    <div class="opacity-60">
      <slot />
    </div>
  </div>
  <slot v-else />
</template>
```

### 2. Retry Mechanism

```typescript
// src/composables/useOfflineRecovery.ts
import { watch } from 'vue'
import { useQueryClient } from '@tanstack/vue-query'
import { useSSE } from '@/composables/useSSE'

export function useOfflineRecovery() {
  const { isConnected } = useSSE()
  const queryClient = useQueryClient()

  // On reconnect: invalidate all queries to fetch fresh data
  watch(isConnected, (connected, wasConnected) => {
    if (connected && wasConnected === false) {
      queryClient.invalidateQueries()
    }
  })

  function manualRetry() {
    queryClient.invalidateQueries()
  }

  return { manualRetry }
}
```

### 3. Offline Banner (extended from MEDIA-751)

```vue
<!-- ConnectionStatusBar extended state -->
<template>
  <div v-if="state === 'disconnected'"
       class="fixed top-0 inset-x-0 z-50 bg-destructive/95 text-destructive-foreground
              flex items-center justify-between px-4 py-2 text-sm">
    <div class="flex items-center gap-2">
      <WifiOff class="h-4 w-4" />
      <span>You're offline. Changes won't be saved.</span>
    </div>
    <Button variant="ghost" size="sm" @click="retry" :disabled="retrying">
      <RefreshCw :class="['h-3.5 w-3.5 mr-1', { 'animate-spin': retrying }]" />
      Retry
    </Button>
  </div>
</template>
```

---

## Browser Online/Offline Detection

```typescript
// src/composables/useNetworkStatus.ts
import { ref, onMounted, onUnmounted } from 'vue'

export function useNetworkStatus() {
  const isOnline = ref(navigator.onLine)

  function handleOnline() { isOnline.value = true }
  function handleOffline() { isOnline.value = false }

  onMounted(() => {
    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)
  })

  onUnmounted(() => {
    window.removeEventListener('online', handleOnline)
    window.removeEventListener('offline', handleOffline)
  })

  return { isOnline }
}
```

Combine with SSE status for full picture:
- `navigator.onLine === false` → definitely offline
- SSE disconnected + navigator.onLine → API might be down

---

## Data Freshness

| Data Source | Offline Behavior |
|-------------|-----------------|
| Queue list | Show cached, mark stale |
| Now playing | Show last known state |
| Admin dashboard | Show cached stats |
| Search results | Show "Search unavailable offline" |
| Player commands | Disable buttons, show "Reconnect to control" |

---

## Tasks

- [ ] Create `useNetworkStatus` composable (browser online/offline events)
- [ ] Create `useOfflineRecovery` composable (invalidate queries on reconnect)
- [ ] Create `StaleDataOverlay.vue` wrapper component
- [ ] Extend `ConnectionStatusBar` with retry button
- [ ] Disable player commands when offline
- [ ] Invalidate all TanStack queries on reconnect
- [ ] Unit tests for network status composable
- [ ] Unit tests for offline recovery behavior

---

## Acceptance Criteria

- [ ] Offline banner shows retry button
- [ ] Stale data is dimmed with "Data may be outdated" badge
- [ ] Player command buttons disabled when offline
- [ ] Manual retry re-fetches all data
- [ ] On reconnect, all queries auto-invalidated (fresh data)
- [ ] Browser offline → immediate "Offline" indicator
