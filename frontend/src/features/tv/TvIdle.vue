<script setup lang="ts">
import { ref, onMounted, onUnmounted } from "vue";
import type { QueueItemResponse } from "@/stores/player";
import { config } from "@/config";

const queuePreview = ref<QueueItemResponse[]>([]);
const clock = ref(formatClock());
let clockTimer: ReturnType<typeof setInterval> | null = null;
let pollTimer: ReturnType<typeof setInterval> | null = null;

function formatClock(): string {
  const now = new Date();
  return now.toLocaleTimeString("da-DK", {
    hour: "2-digit",
    minute: "2-digit",
  });
}

async function fetchQueuePreview() {
  try {
    const res = await fetch(`${config.apiBaseUrl}/queue`);
    if (res.ok) {
      const items: QueueItemResponse[] = await res.json();
      queuePreview.value = items.slice(0, 3);
    }
  } catch {
    /* ignore */
  }
}

onMounted(() => {
  fetchQueuePreview();
  clockTimer = setInterval(() => {
    clock.value = formatClock();
  }, 10_000);
  pollTimer = setInterval(fetchQueuePreview, 15_000);
});

onUnmounted(() => {
  if (clockTimer) clearInterval(clockTimer);
  if (pollTimer) clearInterval(pollTimer);
});
</script>

<template>
  <div
    class="flex h-screen w-screen flex-col items-center justify-center bg-linear-to-br from-[#0a0a1a] via-[#0f0f2e] to-[#0a0a1a]"
  >
    <div class="animate-pulse-slow text-center">
      <p class="text-6xl">🎵</p>
      <h1 class="mt-4 text-4xl font-bold text-white">Media Platform</h1>
      <p class="mt-2 text-lg text-white/50">Waiting for music…</p>
    </div>

    <!-- Queue preview -->
    <div v-if="queuePreview.length > 0" class="mt-12 w-full max-w-md px-6">
      <p
        class="mb-3 text-sm font-medium uppercase tracking-wider text-white/40"
      >
        Next up
      </p>
      <div class="space-y-2">
        <div
          v-for="(item, i) in queuePreview"
          :key="item.id"
          class="flex items-center gap-3 rounded-lg bg-white/5 px-4 py-3"
        >
          <span class="text-sm font-medium text-white/30">{{ i + 1 }}.</span>
          <img
            v-if="item.thumbnailUrl"
            :src="item.thumbnailUrl"
            :alt="item.title"
            class="h-10 w-14 rounded object-cover"
          />
          <div class="min-w-0 flex-1">
            <p class="truncate text-sm text-white/80">{{ item.title }}</p>
            <p v-if="item.channel" class="truncate text-xs text-white/40">
              {{ item.channel }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <p v-else class="mt-12 text-sm text-white/30">
      Add songs from your phone or press OK to search.
    </p>

    <!-- Clock -->
    <div class="absolute bottom-6 right-6 text-lg tabular-nums text-white/30">
      {{ clock }}
    </div>
  </div>
</template>

<style scoped>
@keyframes pulse-slow {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.6;
  }
}
.animate-pulse-slow {
  animation: pulse-slow 4s ease-in-out infinite;
}
</style>
