# MEDIA-751: Global Connection Status Indicator (API / SSE)

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 2 points  
**Status:** ✅ Done  
**Depends on:** MEDIA-704 (SSE composable — provides `isConnected`), MEDIA-707 (layout shell)

---

## Summary

Add a persistent, non-intrusive connection status indicator to the SPA showing whether the API and SSE stream are reachable. Users always know if they're seeing live data or stale state.

---

## States

| State        | Visual                                | Trigger                            |
| ------------ | ------------------------------------- | ---------------------------------- |
| Connected    | Hidden (no indicator)                 | SSE open + last API response < 30s |
| Connecting   | 🟡 pulsing dot + "Connecting..."      | SSE reconnecting                   |
| Disconnected | 🔴 dot + "Offline"                    | SSE closed + API unreachable       |
| Reconnected  | 🟢 dot + "Back online" (2s then hide) | SSE reconnected after disconnect   |

---

## Component

```vue
<!-- src/shared/components/ConnectionStatusBar.vue -->
<script setup lang="ts">
import { useSSEStatus } from "@/composables/useSSEStatus";

const { state, message } = useSSEStatus();
</script>

<template>
  <Transition name="slide-down">
    <div
      v-if="state !== 'connected'"
      class="fixed top-0 inset-x-0 z-50 flex items-center justify-center gap-2 py-1.5 text-sm font-medium"
      :class="{
        'bg-yellow-500/90 text-yellow-950': state === 'connecting',
        'bg-destructive/90 text-destructive-foreground':
          state === 'disconnected',
        'bg-green-500/90 text-green-950': state === 'reconnected',
      }"
    >
      <span
        class="h-2 w-2 rounded-full"
        :class="{
          'bg-yellow-950 animate-pulse': state === 'connecting',
          'bg-destructive-foreground': state === 'disconnected',
          'bg-green-950': state === 'reconnected',
        }"
      />
      {{ message }}
    </div>
  </Transition>
</template>
```

---

## SSE Status Composable

```typescript
// src/composables/useSSEStatus.ts
import { computed, ref, watch } from "vue";
import { useSSE } from "@/composables/useSSE";

type ConnectionState =
  | "connected"
  | "connecting"
  | "disconnected"
  | "reconnected";

export function useSSEStatus() {
  const { isConnected, isReconnecting } = useSSE();
  const wasDisconnected = ref(false);
  const showReconnected = ref(false);

  const state = computed<ConnectionState>(() => {
    if (showReconnected.value) return "reconnected";
    if (isReconnecting.value) return "connecting";
    if (!isConnected.value) return "disconnected";
    return "connected";
  });

  // Track disconnect → reconnect transition
  watch(isConnected, (connected) => {
    if (!connected) {
      wasDisconnected.value = true;
    } else if (wasDisconnected.value) {
      showReconnected.value = true;
      wasDisconnected.value = false;
      setTimeout(() => {
        showReconnected.value = false;
      }, 2000);
    }
  });

  const message = computed(() => {
    switch (state.value) {
      case "connecting":
        return "Connecting...";
      case "disconnected":
        return "Offline — changes may not be visible";
      case "reconnected":
        return "Back online";
      default:
        return "";
    }
  });

  return { state, message };
}
```

---

## Placement

```vue
<!-- src/App.vue -->
<template>
  <ConnectionStatusBar />
  <AppLayout>
    <RouterView />
  </AppLayout>
</template>
```

The bar sits above everything (`z-50`, `fixed top-0`), pushing content down is optional — it overlays to avoid layout shifts.

---

## Tasks

- [ ] Create `useSSEStatus` composable wrapping `useSSE`
- [ ] Create `ConnectionStatusBar.vue` with 4-state visual
- [ ] Add slide-down CSS transition
- [ ] Place in `App.vue` above layout
- [ ] Auto-hide "Back online" after 2 seconds
- [ ] Unit tests for state transitions (connected ↔ connecting ↔ disconnected ↔ reconnected)

---

## Acceptance Criteria

- [ ] No indicator visible when connected (clean UI)
- [ ] Yellow "Connecting..." bar during SSE reconnect
- [ ] Red "Offline" bar when API/SSE unreachable
- [ ] Green "Back online" flash for 2s after reconnect
- [ ] Bar doesn't cause layout shift (fixed position, overlay)
