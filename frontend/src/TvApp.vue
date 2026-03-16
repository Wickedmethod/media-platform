<script setup lang="ts">
import { ref, computed, watch, onUnmounted } from "vue";
import { usePlayerStore } from "@/stores/player";
import { useSSE } from "@/composables/useSSE";
import { config } from "@/config";
import TvPlayer from "@/features/tv/TvPlayer.vue";
import TvOverlay from "@/features/tv/TvOverlay.vue";
import TvIdle from "@/features/tv/TvIdle.vue";
import TvError from "@/features/tv/TvError.vue";

const player = usePlayerStore();
const overlayRef = ref<InstanceType<typeof TvOverlay>>();

// SSE connection — shared composable piping events into player store
const sse = useSSE({
  url: config.apiEventsUrl,
  onEvent: (event, data) => {
    player.handleSSEEvent(event, data);
  },
});

watch(
  [sse.connected, sse.isReconnecting],
  ([connected, reconnecting]) => {
    player.setSseStatus(connected, reconnecting);
  },
  { immediate: true },
);

sse.connect();

// Screen state
type TvScreen = "idle" | "playing" | "error";

const screen = computed<TvScreen>(() => {
  if (player.lastError) return "error";
  if (player.currentItem && !player.isIdle) return "playing";
  return "idle";
});

// Kill switch overlay
const killSwitchActive = computed(() => player.isKillSwitchActive);

// Keyboard handler — toggle overlay with space/enter
function onKeyDown(e: KeyboardEvent) {
  if (e.key === " " || e.key === "Enter") {
    e.preventDefault();
    overlayRef.value?.toggleOverlay();
  }
}

if (typeof window !== "undefined") {
  window.addEventListener("keydown", onKeyDown);
}

onUnmounted(() => {
  window.removeEventListener("keydown", onKeyDown);
  sse.disconnect();
});
</script>

<template>
  <div class="relative h-screen w-screen cursor-none overflow-hidden bg-black">
    <!-- Kill switch overlay -->
    <div
      v-if="killSwitchActive"
      class="absolute inset-0 z-50 flex items-center justify-center bg-black"
    >
      <div class="text-center">
        <p class="text-5xl">🔇</p>
        <p class="mt-4 text-xl text-white/60">Playback disabled by admin</p>
      </div>
    </div>

    <!-- Idle screen -->
    <TvIdle v-if="screen === 'idle'" />

    <!-- Playing screen -->
    <template v-else-if="screen === 'playing'">
      <TvPlayer />
      <TvOverlay ref="overlayRef" />
    </template>

    <!-- Error screen -->
    <TvError v-else-if="screen === 'error'" />

    <!-- Connection indicator -->
    <div
      v-if="!sse.connected.value"
      class="absolute left-4 top-4 z-30 flex items-center gap-2 rounded-full bg-red-500/80 px-3 py-1 text-xs text-white"
    >
      <span class="inline-block h-2 w-2 animate-pulse rounded-full bg-white" />
      {{ sse.isReconnecting.value ? "Reconnecting…" : "Disconnected" }}
    </div>
  </div>
</template>
