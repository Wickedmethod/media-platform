<script setup lang="ts">
import { computed } from 'vue'
import { Play, Pause, SkipForward } from 'lucide-vue-next'
import { usePlayerStore } from '@/stores/player'

const player = usePlayerStore()

const formattedPosition = computed(() => formatTime(player.position))
const formattedDuration = computed(() => formatTime(player.duration))

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60)
  const s = Math.floor(seconds % 60)
  return `${m}:${s.toString().padStart(2, '0')}`
}
</script>

<template>
  <div v-if="player.currentItem" class="border-t border-border bg-card">
    <!-- Progress bar -->
    <div class="h-0.5 w-full bg-muted">
      <div
        class="h-full bg-primary transition-[width] duration-1000 ease-linear"
        :style="{ width: `${player.progress}%` }"
      />
    </div>

    <div class="flex items-center gap-3 px-4 py-2">
      <!-- Track info -->
      <div class="min-w-0 flex-1">
        <p class="truncate text-sm font-medium">{{ player.currentItem.title }}</p>
        <p class="text-xs text-muted-foreground">
          {{ formattedPosition }} / {{ formattedDuration }}
        </p>
      </div>

      <!-- Mini controls -->
      <div class="flex items-center gap-1">
        <button
          class="rounded-full p-1.5 text-muted-foreground transition-colors hover:text-foreground"
          :aria-label="player.isPlaying ? 'Pause' : 'Play'"
        >
          <Pause v-if="player.isPlaying" class="h-4 w-4" />
          <Play v-else class="h-4 w-4" />
        </button>
        <button
          class="rounded-full p-1.5 text-muted-foreground transition-colors hover:text-foreground"
          aria-label="Skip"
        >
          <SkipForward class="h-4 w-4" />
        </button>
      </div>
    </div>
  </div>
</template>
