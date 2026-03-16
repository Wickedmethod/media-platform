<script setup lang="ts">
import { ref, computed, onUnmounted } from "vue";
import { usePlayerStore } from "@/stores/player";
import { useTvSSE } from "@/features/tv/composables/useTvSSE";
import { useCecRemote } from "@/features/tv/composables/useCecRemote";
import { config } from "@/config";
import TvPlayer from "@/features/tv/TvPlayer.vue";
import TvOverlay from "@/features/tv/TvOverlay.vue";
import TvIdle from "@/features/tv/TvIdle.vue";
import TvError from "@/features/tv/TvError.vue";

const player = usePlayerStore();
const overlayRef = ref<InstanceType<typeof TvOverlay>>();
const errorRef = ref<InstanceType<typeof TvError>>();

// SSE connection — TV-specific with infinite reconnect + /sync recovery
const sse = useTvSSE();
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

// CEC Remote Control — maps keyboard/CEC events to actions
useCecRemote({
  onPlay: () => {
    fetch(`${config.apiBaseUrl}/player/play`, { method: "POST" }).catch(
      () => {},
    );
  },
  onPause: () => {
    fetch(`${config.apiBaseUrl}/player/pause`, { method: "POST" }).catch(
      () => {},
    );
  },
  onSkip: () => {
    if (screen.value === "error") {
      errorRef.value?.skipToNext();
    } else {
      fetch(`${config.apiBaseUrl}/player/skip`, { method: "POST" }).catch(
        () => {},
      );
    }
  },
  onRestart: () => {
    fetch(`${config.apiBaseUrl}/player/play`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ startAtSeconds: 0 }),
    }).catch(() => {});
  },
  onStop: () => {
    fetch(`${config.apiBaseUrl}/player/stop`, { method: "POST" }).catch(
      () => {},
    );
  },
  onToggleOverlay: () => {
    overlayRef.value?.toggleOverlay();
  },
});

onUnmounted(() => {
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
    <TvError v-else-if="screen === 'error'" ref="errorRef" @skip="player.lastError = null" />

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
