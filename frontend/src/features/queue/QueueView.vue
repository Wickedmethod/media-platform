<script setup lang="ts">
import { useQueryClient } from "@tanstack/vue-query";
import { useGetQueue, useAddToQueue, useRemoveFromQueue, useGetQueueMode, useSetQueueMode, getGetQueueQueryKey, getGetQueueModeQueryKey } from "@/generated/queue/queue";
import { usePlay, usePause, useSkip, useStop } from "@/generated/player/player";
import { usePlayerStore, type QueueMode } from "@/stores/player";
import { useAuthStore } from "@/stores/auth";
import { useSSE } from "@/composables/useSSE";
import { useToast } from "@/composables/useToast";
import { config } from "@/config";
import type { QueueItemResponse } from "@/generated/models";
import NowPlaying from "./NowPlaying.vue";
import QueueList from "./QueueList.vue";
import AddToQueue from "./AddToQueue.vue";
import PlayerControls from "./PlayerControls.vue";
import QueueModeSelector from "./QueueModeSelector.vue";

const queryClient = useQueryClient();
const player = usePlayerStore();
const auth = useAuthStore();
const toast = useToast();

// --- Data fetching ---
const { data: queueItems, isLoading: queueLoading } = useGetQueue();
const { data: queueMode } = useGetQueueMode();

// --- Mutations ---
const addMutation = useAddToQueue({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetQueueQueryKey() });
      toast.success("Added to queue");
    },
    onError: () => toast.error("Failed to add to queue"),
  },
});

const removeMutation = useRemoveFromQueue({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetQueueQueryKey() });
      toast.success("Removed from queue");
    },
    onError: () => toast.error("Failed to remove item"),
  },
});

const setModeMutation = useSetQueueMode({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetQueueModeQueryKey() });
    },
  },
});

const playMutation = usePlay();
const pauseMutation = usePause();
const skipMutation = useSkip();
const stopMutation = useStop();

// --- SSE for real-time updates ---
useSSE({
  url: `${config.apiBaseUrl}/events`,
  withCredentials: true,
  onEvent: (event, data) => {
    player.handleSSEEvent(event, data);

    // Invalidate queue on queue-related events
    if (event === "queue-updated" || event === "item-added") {
      queryClient.invalidateQueries({ queryKey: getGetQueueQueryKey() });
    }
  },
});

// --- Handlers ---
function handleAdd(url: string, title: string) {
  addMutation.mutate({ data: { url, title } });
}

function handleRemove(id: string) {
  removeMutation.mutate({ id });
}

function handleModeChange(mode: QueueMode) {
  setModeMutation.mutate({ data: { mode } });
}
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-4 p-4">
    <!-- Now Playing -->
    <NowPlaying />

    <!-- Admin controls -->
    <div v-if="auth.isAdmin" class="flex flex-wrap items-center gap-3">
      <PlayerControls
        :play-loading="playMutation.isPending.value"
        :pause-loading="pauseMutation.isPending.value"
        :skip-loading="skipMutation.isPending.value"
        :stop-loading="stopMutation.isPending.value"
        @play="playMutation.mutate()"
        @pause="pauseMutation.mutate()"
        @skip="skipMutation.mutate()"
        @stop="stopMutation.mutate()"
      />
      <QueueModeSelector
        :current-mode="queueMode?.mode ?? 'Sequential'"
        @change="handleModeChange"
      />
    </div>

    <!-- Add to queue -->
    <AddToQueue @add="handleAdd" />

    <!-- Queue list -->
    <div>
      <h2 class="mb-2 text-sm font-semibold text-muted-foreground">
        Queue ({{ queueItems?.length ?? 0 }} items)
      </h2>
      <QueueList
        :items="(queueItems as QueueItemResponse[] | undefined) ?? []"
        :is-loading="queueLoading"
        @remove="handleRemove"
      />
    </div>
  </div>
</template>
