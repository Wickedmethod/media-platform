<script setup lang="ts">
import { ref, watch, onUnmounted, computed } from "vue";
import { usePlayerStore } from "@/stores/player";

const playerStore = usePlayerStore();
const visible = ref(false);
let hideTimer: ReturnType<typeof setTimeout> | null = null;

const SHOW_DURATION_MS = 5000;

const progressPercent = computed(() => playerStore.progress);

function showOverlay() {
  visible.value = true;
  if (hideTimer) clearTimeout(hideTimer);
  hideTimer = setTimeout(() => {
    visible.value = false;
  }, SHOW_DURATION_MS);
}

function toggleOverlay() {
  if (visible.value) {
    visible.value = false;
    if (hideTimer) clearTimeout(hideTimer);
  } else {
    showOverlay();
  }
}

// Show overlay on track change
watch(
  () => playerStore.currentItem,
  (item) => {
    if (item) showOverlay();
  },
);

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return `${m}:${s.toString().padStart(2, "0")}`;
}

onUnmounted(() => {
  if (hideTimer) clearTimeout(hideTimer);
});

defineExpose({ toggleOverlay });
</script>

<template>
  <Transition
    enter-active-class="transition-opacity duration-500"
    leave-active-class="transition-opacity duration-500"
    enter-from-class="opacity-0"
    leave-to-class="opacity-0"
  >
    <div
      v-if="visible && playerStore.currentItem"
      class="absolute inset-x-0 bottom-0 z-20"
    >
      <!-- Progress bar -->
      <div class="h-1 w-full bg-white/20">
        <div
          class="h-full bg-primary transition-[width] duration-1000"
          :style="{ width: `${progressPercent}%` }"
        />
      </div>

      <!-- Info bar -->
      <div class="flex items-center gap-4 bg-black/85 px-6 py-4 backdrop-blur">
        <div class="text-2xl">
          {{ playerStore.isPlaying ? "▶" : "❚❚" }}
        </div>

        <div class="min-w-0 flex-1">
          <p class="truncate text-lg font-medium text-white">
            {{ playerStore.currentItem.title }}
          </p>
          <p class="text-sm text-white/60">
            <span v-if="playerStore.currentItem.addedByName">
              Added by: {{ playerStore.currentItem.addedByName }}
            </span>
          </p>
        </div>

        <div class="text-right text-sm tabular-nums text-white/80">
          <p>{{ formatTime(playerStore.position) }}</p>
          <p v-if="playerStore.duration" class="text-white/40">
            {{ formatTime(playerStore.duration) }}
          </p>
        </div>
      </div>
    </div>
  </Transition>
</template>
