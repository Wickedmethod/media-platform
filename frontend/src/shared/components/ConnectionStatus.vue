<script setup lang="ts">
import type { ConnectionStatus } from "@/composables/useSSE";

const props = defineProps<{
  status: ConnectionStatus;
  reconnectAttempts: number;
}>();

const emit = defineEmits<{
  retry: [];
}>();

const statusConfig: Record<
  ConnectionStatus,
  { label: string; dotClass: string }
> = {
  connected: { label: "Connected", dotClass: "bg-emerald-500" },
  connecting: { label: "Connecting…", dotClass: "bg-amber-500 animate-pulse" },
  reconnecting: {
    label: "Reconnecting…",
    dotClass: "bg-amber-500 animate-pulse",
  },
  disconnected: { label: "Disconnected", dotClass: "bg-muted-foreground" },
  failed: { label: "Offline", dotClass: "bg-red-500" },
};
</script>

<template>
  <div class="flex items-center gap-2 text-xs text-muted-foreground">
    <span
      :class="[
        'inline-block h-2 w-2 rounded-full',
        statusConfig[props.status].dotClass,
      ]"
    />
    <span>{{ statusConfig[props.status].label }}</span>
    <span v-if="props.status === 'reconnecting'" class="tabular-nums">
      ({{ props.reconnectAttempts }})
    </span>
    <button
      v-if="props.status === 'failed'"
      class="underline hover:text-foreground"
      @click="emit('retry')"
    >
      Retry
    </button>
  </div>
</template>
