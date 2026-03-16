<script setup lang="ts">
import { computed } from "vue";
import { GripVertical, Music, Trash2, User, Tv, Clock } from "lucide-vue-next";
import type { QueueItemResponse } from "@/generated/models";
import { useAuthStore } from "@/stores/auth";

const props = defineProps<{
  item: QueueItemResponse;
  index: number;
  draggable?: boolean;
}>();

const emit = defineEmits<{
  remove: [id: string];
  select: [item: QueueItemResponse];
}>();

const auth = useAuthStore();

function canRemove(): boolean {
  if (auth.isAdmin) return true;
  return props.item.addedByUserId === auth.user?.id;
}

const isTV = computed(() => props.item.addedByUserId === "tv-guest");

const addedAgo = computed(() => {
  const ms = Date.now() - new Date(props.item.addedAt).getTime();
  const mins = Math.floor(ms / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins} min ago`;
  const hrs = Math.floor(mins / 60);
  return `${hrs}h ago`;
});

function formatDuration(seconds?: number | string | null): string {
  if (!seconds) return "";
  const s = typeof seconds === "string" ? parseInt(seconds, 10) : seconds;
  if (isNaN(s) || s <= 0) return "";
  const m = Math.floor(s / 60);
  const sec = s % 60;
  return `${m}:${sec.toString().padStart(2, "0")}`;
}
</script>

<template>
  <div
    class="group flex items-center gap-3 rounded-lg border border-border bg-card p-3 transition-colors hover:bg-accent/50 cursor-pointer"
    @click="emit('select', item)"
  >
    <!-- Drag handle (admin only) -->
    <span
      v-if="draggable"
      class="drag-handle flex cursor-grab items-center text-muted-foreground"
    >
      <GripVertical class="h-4 w-4" />
    </span>

    <!-- Index -->
    <span class="w-5 text-center text-xs tabular-nums text-muted-foreground">
      {{ index + 1 }}
    </span>

    <!-- Thumbnail -->
    <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded bg-muted overflow-hidden">
      <img
        v-if="item.thumbnailUrl"
        :src="item.thumbnailUrl"
        :alt="item.title"
        class="h-full w-full object-cover"
      />
      <Music v-else class="h-4 w-4 text-muted-foreground" />
    </div>

    <!-- Info -->
    <div class="min-w-0 flex-1">
      <p class="truncate text-sm font-medium">{{ item.title }}</p>
      <div class="flex items-center gap-2 text-xs text-muted-foreground">
        <span v-if="item.channel" class="truncate">{{ item.channel }}</span>
        <span v-if="formatDuration(item.durationSeconds)" class="flex items-center gap-0.5">
          <Clock class="h-3 w-3" />
          {{ formatDuration(item.durationSeconds) }}
        </span>
        <span class="flex items-center gap-0.5">
          <Tv v-if="isTV" class="h-3 w-3" />
          <User v-else class="h-3 w-3" />
          {{ item.addedByName ?? "Unknown" }}
        </span>
        <span>&middot;</span>
        <span>{{ addedAgo }}</span>
      </div>
    </div>

    <!-- Remove button -->
    <button
      v-if="canRemove()"
      class="rounded-md p-1.5 text-muted-foreground opacity-0 transition-all hover:bg-destructive/10 hover:text-destructive group-hover:opacity-100"
      aria-label="Remove from queue"
      @click.stop="emit('remove', item.id)"
    >
      <Trash2 class="h-4 w-4" />
    </button>
  </div>
</template>
