<script setup lang="ts">
import { Play, Pause, SkipForward, X } from "lucide-vue-next";
import type { SessionPlaybackState, SessionQueueItem } from "./api";

defineProps<{
  playback: SessionPlaybackState;
  queueCount: number;
}>();

const emit = defineEmits<{
  play: [];
  pause: [];
  skip: [];
  stop: [];
}>();
</script>

<template>
  <div v-if="playback.currentItem" class="border-t border-primary/20 bg-primary/5">
    <div class="flex items-center gap-3 px-4 py-2">
      <!-- Headphone indicator -->
      <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
        <span class="text-xs font-bold">🎧</span>
      </div>

      <!-- Track info -->
      <div class="min-w-0 flex-1">
        <p class="truncate text-sm font-medium">
          {{ playback.currentItem.title }}
        </p>
        <p class="text-xs text-muted-foreground">
          Personal · {{ queueCount }} in queue
        </p>
      </div>

      <!-- Controls -->
      <div class="flex items-center gap-1">
        <button
          class="rounded-full p-1.5 text-muted-foreground transition-colors hover:text-foreground"
          :aria-label="playback.state === 'Playing' ? 'Pause' : 'Play'"
          @click="playback.state === 'Playing' ? emit('pause') : emit('play')"
        >
          <Pause v-if="playback.state === 'Playing'" class="h-4 w-4" />
          <Play v-else class="h-4 w-4" />
        </button>
        <button
          class="rounded-full p-1.5 text-muted-foreground transition-colors hover:text-foreground"
          aria-label="Skip"
          @click="emit('skip')"
        >
          <SkipForward class="h-4 w-4" />
        </button>
        <button
          class="rounded-full p-1.5 text-red-400 transition-colors hover:text-red-500"
          aria-label="End session"
          @click="emit('stop')"
        >
          <X class="h-4 w-4" />
        </button>
      </div>
    </div>
  </div>
</template>
