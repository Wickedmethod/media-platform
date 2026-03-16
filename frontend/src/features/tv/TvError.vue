<script setup lang="ts">
import { usePlayerStore } from "@/stores/player";

const playerStore = usePlayerStore();
</script>

<template>
  <div
    class="flex h-screen w-screen flex-col items-center justify-center bg-[#0a0a1a] text-white"
  >
    <p class="text-6xl">⚠️</p>
    <h2 class="mt-4 text-2xl font-bold">Playback Error</h2>
    <p class="mt-2 text-lg text-white/60">
      {{ playerStore.lastError ?? "An unknown error occurred" }}
    </p>
    <p class="mt-6 text-sm text-white/30">
      The player will automatically retry or skip to the next track.
    </p>
  </div>
</template>
