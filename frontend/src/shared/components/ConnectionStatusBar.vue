<script setup lang="ts">
import { computed, inject } from "vue";
import { RefreshCw } from "lucide-vue-next";
import { useSSEStatus } from "@/composables/useSSEStatus";
import { useNetworkStatus } from "@/composables/useNetworkStatus";
import { Button } from "@/shared/components/ui/button";

const { state: sseState, message: sseMessage } = useSSEStatus();
const { isOnline } = useNetworkStatus();
const reconnect = inject<() => void>("sseReconnect");

const state = computed(() => {
  if (!isOnline.value) return "offline" as const;
  return sseState.value;
});

const message = computed(() => {
  if (!isOnline.value) return "No internet connection";
  return sseMessage.value;
});

const showRetry = computed(
  () => state.value === "disconnected" || state.value === "offline",
);

function handleRetry() {
  reconnect?.();
}
</script>

<template>
  <Transition name="slide-down">
    <div
      v-if="state !== 'connected'"
      class="fixed inset-x-0 top-0 z-50 flex items-center justify-center gap-2 py-1.5 text-sm font-medium"
      :class="{
        'bg-yellow-500/90 text-yellow-950': state === 'connecting',
        'bg-destructive/90 text-destructive-foreground':
          state === 'disconnected' || state === 'offline',
        'bg-green-500/90 text-green-950': state === 'reconnected',
      }"
    >
      <span
        class="h-2 w-2 rounded-full"
        :class="{
          'bg-yellow-950 animate-pulse': state === 'connecting',
          'bg-destructive-foreground':
            state === 'disconnected' || state === 'offline',
          'bg-green-950': state === 'reconnected',
        }"
      />
      {{ message }}
      <Button
        v-if="showRetry"
        variant="ghost"
        size="sm"
        class="ml-1 h-6 px-2 text-xs"
        @click="handleRetry"
      >
        <RefreshCw class="mr-1 h-3 w-3" />
        Retry
      </Button>
    </div>
  </Transition>
</template>

<style scoped>
.slide-down-enter-active,
.slide-down-leave-active {
  transition:
    transform 0.3s ease,
    opacity 0.3s ease;
}

.slide-down-enter-from,
.slide-down-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}
</style>
