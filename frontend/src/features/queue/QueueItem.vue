<script setup lang="ts">
import { Music, Trash2, User } from "lucide-vue-next";
import type { QueueItemResponse } from "@/generated/models";
import { useAuthStore } from "@/stores/auth";

const props = defineProps<{
  item: QueueItemResponse;
  index: number;
}>();

const emit = defineEmits<{
  remove: [id: string];
}>();

const auth = useAuthStore();

function canRemove(): boolean {
  if (auth.isAdmin) return true;
  return props.item.addedByUserId === auth.user?.id;
}
</script>

<template>
  <div
    class="group flex items-center gap-3 rounded-lg border border-border bg-card p-3 transition-colors hover:bg-accent/50"
  >
    <!-- Index -->
    <span class="w-5 text-center text-xs tabular-nums text-muted-foreground">
      {{ index + 1 }}
    </span>

    <!-- Thumbnail -->
    <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded bg-muted">
      <Music class="h-4 w-4 text-muted-foreground" />
    </div>

    <!-- Info -->
    <div class="min-w-0 flex-1">
      <p class="truncate text-sm font-medium">{{ item.title }}</p>
      <div class="flex items-center gap-1 text-xs text-muted-foreground">
        <User class="h-3 w-3" />
        <span>{{ item.addedByName ?? "Unknown" }}</span>
      </div>
    </div>

    <!-- Remove button -->
    <button
      v-if="canRemove()"
      class="rounded-md p-1.5 text-muted-foreground opacity-0 transition-all hover:bg-destructive/10 hover:text-destructive group-hover:opacity-100"
      aria-label="Remove from queue"
      @click="emit('remove', item.id)"
    >
      <Trash2 class="h-4 w-4" />
    </button>
  </div>
</template>
