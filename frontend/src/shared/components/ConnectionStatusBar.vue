<script setup lang="ts">
import { useSSEStatus } from "@/composables/useSSEStatus";

const { state, message } = useSSEStatus();
</script>

<template>
  <Transition name="slide-down">
    <div
      v-if="state !== 'connected'"
      class="fixed inset-x-0 top-0 z-50 flex items-center justify-center gap-2 py-1.5 text-sm font-medium"
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
