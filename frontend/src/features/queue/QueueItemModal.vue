<script setup lang="ts">
import { computed } from "vue";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/shared/components/ui/dialog";
import { ExternalLink, Clock, User, Tv, CalendarDays } from "lucide-vue-next";
import type { QueueItemResponse } from "@/generated/models";
import { useAuthStore } from "@/stores/auth";

const props = defineProps<{
  item: QueueItemResponse;
  open: boolean;
}>();

const emit = defineEmits<{
  "update:open": [value: boolean];
  remove: [id: string];
  playNext: [id: string];
}>();

const auth = useAuthStore();
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
  if (!seconds) return "Unknown";
  const s = typeof seconds === "string" ? parseInt(seconds, 10) : seconds;
  if (isNaN(s) || s <= 0) return "Unknown";
  const m = Math.floor(s / 60);
  const sec = s % 60;
  return `${m}:${sec.toString().padStart(2, "0")}`;
}

const thumbnailUrl = computed(() => {
  if (props.item.thumbnailUrl) return props.item.thumbnailUrl;
  // Extract video ID from URL for fallback thumbnail
  const match = props.item.url.match(/[?&]v=([^&]+)/);
  return match ? `https://i.ytimg.com/vi/${match[1]}/mqdefault.jpg` : null;
});
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="max-w-md">
      <DialogHeader>
        <DialogTitle>Queue Item Details</DialogTitle>
      </DialogHeader>

      <!-- Thumbnail -->
      <img
        v-if="thumbnailUrl"
        :src="thumbnailUrl"
        :alt="item.title"
        class="w-full rounded-lg aspect-video object-cover"
      />

      <!-- Title + Channel -->
      <div>
        <h3 class="text-lg font-semibold leading-tight">{{ item.title }}</h3>
        <p v-if="item.channel" class="text-sm text-muted-foreground">{{ item.channel }}</p>
      </div>

      <!-- Metadata -->
      <div class="space-y-1.5 text-sm text-muted-foreground">
        <a
          :href="item.url"
          target="_blank"
          rel="noopener noreferrer"
          class="flex items-center gap-1.5 hover:text-foreground transition-colors"
          @click.stop
        >
          <ExternalLink class="h-3.5 w-3.5 shrink-0" />
          <span class="truncate">{{ item.url }}</span>
        </a>
        <p class="flex items-center gap-1.5">
          <Clock class="h-3.5 w-3.5 shrink-0" />
          {{ formatDuration(item.durationSeconds) }}
        </p>
        <p class="flex items-center gap-1.5">
          <Tv v-if="isTV" class="h-3.5 w-3.5 shrink-0" />
          <User v-else class="h-3.5 w-3.5 shrink-0" />
          Added by {{ item.addedByName ?? "Unknown" }}
        </p>
        <p class="flex items-center gap-1.5">
          <CalendarDays class="h-3.5 w-3.5 shrink-0" />
          {{ addedAgo }}
        </p>
      </div>

      <!-- Admin actions -->
      <div v-if="auth.isAdmin" class="flex gap-2 pt-2">
        <button
          class="flex-1 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors"
          @click="emit('playNext', item.id); emit('update:open', false)"
        >
          Play Next
        </button>
        <button
          class="flex-1 rounded-md bg-destructive px-3 py-2 text-sm font-medium text-destructive-foreground hover:bg-destructive/90 transition-colors"
          @click="emit('remove', item.id); emit('update:open', false)"
        >
          Remove
        </button>
      </div>
    </DialogContent>
  </Dialog>
</template>
