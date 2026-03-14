<script setup lang="ts">
import { useSSEStatus } from "@/composables/useSSEStatus";
import { useNetworkStatus } from "@/composables/useNetworkStatus";
import { computed } from "vue";
import { Badge } from "@/shared/components/ui/badge";

const { state } = useSSEStatus();
const { isOnline } = useNetworkStatus();

const isStale = computed(
  () => !isOnline.value || state.value === "disconnected",
);
</script>

<template>
  <div class="relative">
    <div :class="{ 'opacity-60 pointer-events-none': isStale }">
      <slot />
    </div>
    <Transition name="fade">
      <div
        v-if="isStale"
        class="absolute inset-0 flex items-start justify-center pt-8"
      >
        <Badge variant="secondary" class="backdrop-blur-sm">
          Data may be outdated
        </Badge>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
