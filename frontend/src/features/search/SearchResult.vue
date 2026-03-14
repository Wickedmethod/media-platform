<script setup lang="ts">
import { Music, Plus, Loader2 } from "lucide-vue-next";
import type { SearchResult } from "@/composables/useYouTubeSearch";

const props = defineProps<{
  result: SearchResult;
  isAdding?: boolean;
}>();

const emit = defineEmits<{
  add: [result: SearchResult];
}>();

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}
</script>

<template>
  <div class="group flex items-center gap-3 rounded-lg p-2 hover:bg-accent">
    <div
      class="relative h-16 w-24 flex-shrink-0 overflow-hidden rounded-md bg-muted"
    >
      <img
        v-if="props.result.thumbnailUrl"
        :src="props.result.thumbnailUrl"
        :alt="props.result.title"
        class="h-full w-full object-cover"
        loading="lazy"
      />
      <div v-else class="flex h-full w-full items-center justify-center">
        <Music class="h-6 w-6 text-muted-foreground" />
      </div>
      <span
        v-if="props.result.duration > 0"
        class="absolute bottom-0.5 right-0.5 rounded bg-black/80 px-1 text-[10px] font-medium text-white"
      >
        {{ formatDuration(props.result.duration) }}
      </span>
    </div>

    <div class="min-w-0 flex-1">
      <p class="truncate text-sm font-medium">{{ props.result.title }}</p>
      <p class="truncate text-xs text-muted-foreground">
        {{ props.result.channel }}
      </p>
    </div>

    <button
      class="flex-shrink-0 rounded-full p-2 text-muted-foreground hover:bg-primary hover:text-primary-foreground"
      :disabled="props.isAdding"
      @click="emit('add', props.result)"
    >
      <Loader2 v-if="props.isAdding" class="h-5 w-5 animate-spin" />
      <Plus v-else class="h-5 w-5" />
    </button>
  </div>
</template>
