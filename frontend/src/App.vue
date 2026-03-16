<script setup lang="ts">
import { watch, provide } from "vue";
import { RouterView } from "vue-router";
import ErrorBoundary from "@/shared/components/ErrorBoundary.vue";
import ToastContainer from "@/shared/components/ToastContainer.vue";
import AppSidebar from "@/shared/components/AppSidebar.vue";
import BottomTabBar from "@/shared/components/BottomTabBar.vue";
import NowPlayingBar from "@/shared/components/NowPlayingBar.vue";
import ConnectionStatusBar from "@/shared/components/ConnectionStatusBar.vue";
import { usePlayerStore } from "@/stores/player";
import { useSSE } from "@/composables/useSSE";
import { useOfflineRecovery } from "@/composables/useOfflineRecovery";
import { config } from "@/config";

const player = usePlayerStore();

// Global SSE connection — pipes events to player store
const sse = useSSE({
  url: config.apiEventsUrl,
  withCredentials: true,
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

// Invalidate queries on SSE reconnect
const { manualRetry } = useOfflineRecovery();

// Provide reconnect action for ConnectionStatusBar
provide("sseReconnect", () => {
  sse.connect();
  manualRetry();
});
</script>

<template>
  <div class="min-h-screen bg-background text-foreground">
    <ConnectionStatusBar />
    <AppSidebar />

    <main class="pb-24 md:pb-16 md:pl-56">
      <ErrorBoundary>
        <RouterView />
      </ErrorBoundary>
    </main>

    <!-- Mobile: NowPlaying + BottomTabBar -->
    <div class="fixed inset-x-0 bottom-0 z-50 md:hidden">
      <NowPlayingBar v-if="player.currentItem" />
      <BottomTabBar />
    </div>

    <!-- Desktop: NowPlaying bar at bottom -->
    <NowPlayingBar
      v-if="player.currentItem"
      class="fixed inset-x-0 bottom-0 left-56 z-40 hidden md:block"
    />

    <ToastContainer />
  </div>
</template>
