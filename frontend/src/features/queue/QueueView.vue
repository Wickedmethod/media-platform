<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useQueryClient } from "@tanstack/vue-query";
import { onUnmounted } from "vue";
import {
  useGetQueue,
  useAddToQueue,
  useRemoveFromQueue,
  useGetQueueMode,
  useSetQueueMode,
  useReorderQueue,
  getGetQueueQueryKey,
  getGetQueueModeQueryKey,
} from "@/generated/queue/queue";
import {
  usePlayerStore,
  type QueueMode,
  type SSEEventPayloads,
} from "@/stores/player";
import { useAuthStore } from "@/stores/auth";
import { useToast } from "@/composables/useToast";
import { usePlayerCommands } from "@/composables/usePlayerCommands";
import { usePersonalSession } from "@/features/session/usePersonalSession";
import PersonalPlayer from "@/features/session/PersonalPlayer.vue";
import PersonalPlayerBar from "@/features/session/PersonalPlayerBar.vue";
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
const session = usePersonalSession();

// --- Tab state ---
const activeTab = ref<"party" | "personal">("party");

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
    toast.info(`${d.addedByName ?? "Someone"} added a song`, d.title);
  }
});
onUnmounted(unsubscribe);

// --- Handlers ---
function handleAdd(url: string, title: string) {
  if (activeTab.value === "personal" && session.active.value) {
    session.addToQueue(url, title);
  } else {
    addMutation.mutate({ data: { url, title } });
  }
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

function handleCopyToPersonal(item: QueueItemResponse) {
  if (!session.active.value) return;
  session.addToQueue(item.url, item.title);
  toast.success("Copied to personal queue");
}

function handleTrackEnd() {
  session.skip();
}

function handlePlayerError(_code: number) {
  session.skip();
}

// Try to resume existing session on mount
onMounted(() => session.tryResume());
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-4 p-4">
    <!-- Now Playing (party mode) -->
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

    <!-- Queue tabs -->
    <div class="flex items-center gap-2 border-b border-border">
      <button
        class="px-3 py-2 text-sm font-medium transition-colors"
        :class="activeTab === 'party'
          ? 'border-b-2 border-primary text-foreground'
          : 'text-muted-foreground hover:text-foreground'"
        @click="activeTab = 'party'"
      >
        🎉 Party Queue
      </button>
      <button
        class="px-3 py-2 text-sm font-medium transition-colors"
        :class="activeTab === 'personal'
          ? 'border-b-2 border-primary text-foreground'
          : 'text-muted-foreground hover:text-foreground'"
        @click="activeTab = 'personal'"
      >
        🎧 My Queue
        <span v-if="session.active.value" class="ml-1 inline-block h-1.5 w-1.5 rounded-full bg-green-500" />
      </button>
    </div>

    <!-- Party Queue Tab -->
    <template v-if="activeTab === 'party'">
      <AddToQueue @add="handleAdd" />

      <div>
        <h2 class="mb-2 text-sm font-semibold text-muted-foreground">
          Queue ({{ queueItems?.length ?? 0 }} items)
        </h2>
        <QueueList
          :items="(queueItems as QueueItemResponse[] | undefined) ?? []"
          :is-loading="queueLoading"
          :show-copy-action="session.active.value"
          @remove="handleRemove"
          @select="handleSelect"
          @reorder="handleReorder"
          @copy="handleCopyToPersonal"
        />
      </div>
    </template>

    <!-- Personal Queue Tab -->
    <template v-if="activeTab === 'personal'">
      <!-- Start session button -->
      <div v-if="!session.active.value" class="py-8 text-center">
        <p class="mb-3 text-sm text-muted-foreground">
          Start a personal audio session to listen on this device
        </p>
        <button
          class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
          :disabled="session.loading.value"
          @click="session.start()"
        >
          {{ session.loading.value ? "Starting…" : "🎧 Start Personal Session" }}
        </button>
      </div>

      <!-- Active session -->
      <template v-else>
        <AddToQueue @add="handleAdd" />

        <div>
          <h2 class="mb-2 text-sm font-semibold text-muted-foreground">
            My Queue ({{ session.queue.value.length }} items)
          </h2>
          <div v-if="session.queue.value.length === 0" class="py-6 text-center text-sm text-muted-foreground">
            Your personal queue is empty. Add songs or copy from the party queue.
          </div>
          <ul v-else class="space-y-1">
            <li
              v-for="item in session.queue.value"
              :key="item.id"
              class="flex items-center gap-3 rounded-lg p-2 hover:bg-muted/50"
            >
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-medium">{{ item.title }}</p>
                <p v-if="item.channel" class="truncate text-xs text-muted-foreground">
                  {{ item.channel }}
                </p>
              </div>
            </li>
          </ul>
        </div>
      </template>
    </template>

    <!-- Details modal -->
    <QueueItemModal
      v-if="selectedItem"
      :item="selectedItem"
      :open="modalOpen"
      @update:open="modalOpen = $event"
      @remove="handleRemove"
    />

    <!-- Personal player (audio-only, hidden) -->
    <PersonalPlayer
      v-if="session.active.value"
      :video-url="session.currentItem.value?.url ?? null"
      :start-at="session.playback.value.positionSeconds"
      :is-playing="session.isPlaying.value"
      @track-end="handleTrackEnd"
      @error="handlePlayerError"
    />

    <!-- Personal player mini-bar -->
    <PersonalPlayerBar
      v-if="session.active.value && session.currentItem.value"
      :playback="session.playback.value"
      :queue-count="session.queue.value.length"
      class="fixed inset-x-0 bottom-14 z-40 md:bottom-0 md:left-56"
      @play="session.play()"
      @pause="session.pause()"
      @skip="session.skip()"
      @stop="session.stop()"
    />
  </div>
</template>
