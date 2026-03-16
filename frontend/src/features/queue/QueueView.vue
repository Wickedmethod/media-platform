<script setup lang="ts">
import { ref } from "vue";
import { useQueryClient } from "@tanstack/vue-query";
import { onUnmounted } from "vue";
import { useGetQueue, useAddToQueue, useRemoveFromQueue, useGetQueueMode, useSetQueueMode, useReorderQueue, getGetQueueQueryKey, getGetQueueModeQueryKey } from "@/generated/queue/queue";
import { usePlayerStore, type QueueMode, type SSEEventPayloads } from "@/stores/player";
import { useAuthStore } from "@/stores/auth";
import { useToast } from "@/composables/useToast";
import { usePlayerCommands } from "@/composables/usePlayerCommands";
import type { QueueItemResponse } from "@/generated/models";
import NowPlaying from "./NowPlaying.vue";
import QueueList from "./QueueList.vue";
import AddToQueue from "./AddToQueue.vue";
import PlayerControls from "./PlayerControls.vue";
import QueueModeSelector from "./QueueModeSelector.vue";
import QueueItemModal from "./QueueItemModal.vue";

const queryClient = useQueryClient();
const player = usePlayerStore();
const auth = useAuthStore();
const toast = useToast();

// --- Modal state ---
const selectedItem = ref<QueueItemResponse | null>(null);
const modalOpen = ref(false);

function handleSelect(item: QueueItemResponse) {
  selectedItem.value = item;
  modalOpen.value = true;
}

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

const reorderMutation = useReorderQueue({
  mutation: {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: getGetQueueQueryKey() });
    },
    onError: () => {
      toast.error("Failed to reorder — queue may have changed");
      queryClient.invalidateQueries({ queryKey: getGetQueueQueryKey() });
    },
  },
});

const commands = usePlayerCommands();

// --- SSE-driven query invalidation + toasts ---
const unsubscribe = player.onSSEEvent((event, data) => {
  if (event === "queue-updated" || event === "item-added") {
    queryClient.invalidateQueries({ queryKey: getGetQueueQueryKey() });
  }
  if (event === "item-added") {
    const d = data as SSEEventPayloads["item-added"];
    toast.info(
      `${d.addedByName ?? "Someone"} added a song`,
      d.title,
    );
  }
});
onUnmounted(unsubscribe);

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

function handleReorder(itemId: string, newIndex: number) {
  reorderMutation.mutate({ data: { itemId, newIndex } });
}
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-4 p-4">
    <!-- Now Playing -->
    <NowPlaying />

    <!-- Admin controls -->
    <div v-if="auth.isAdmin" class="flex flex-wrap items-center gap-3">
      <PlayerControls
        :pending-command="commands.pendingCommand.value"
        :is-disabled="commands.isDisabled.value"
        @play="commands.play()"
        @pause="commands.pause()"
        @skip="commands.skip()"
        @stop="commands.stop()"
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
        @select="handleSelect"
        @reorder="handleReorder"
      />
    </div>

    <!-- Details modal -->
    <QueueItemModal
      v-if="selectedItem"
      :item="selectedItem"
      :open="modalOpen"
      @update:open="modalOpen = $event"
      @remove="handleRemove"
    />
  </div>
</template>
