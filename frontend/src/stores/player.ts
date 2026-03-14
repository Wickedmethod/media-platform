import { defineStore } from "pinia";
import { ref, computed } from "vue";
import type { SSEEventType } from "@/composables/useSSE";

/** Matches backend QueueItemResponse */
export interface QueueItemResponse {
  id: string;
  title: string;
  url: string;
  thumbnailUrl?: string;
  duration?: number;
  addedByUserId?: string;
  addedByName?: string;
  addedAt: string;
}

export type PlayerState = "Playing" | "Paused" | "Stopped" | "Idle";
export type QueueMode = "Sequential" | "Shuffle" | "RepeatOne" | "RepeatAll";

/** Maps SSE event types to their backend payload shapes */
export interface SSEEventPayloads {
  "state-changed": { state: PlayerState };
  "track-changed": { item: QueueItemResponse; position: number };
  "position-updated": { position: number; duration: number };
  "queue-updated": { action: string; count?: number };
  "item-added": {
    id: string;
    title: string;
    url: string;
    addedByUserId?: string;
    addedByName?: string;
  };
  "kill-switch-toggled": { active: boolean };
  "playback-error": { error: string; videoId: string; retryCount: number };
  "policy-changed": { action: string };
  "player-offline": { playerId: string };
  "player-online": { playerId: string; name: string };
  "player-disconnected": { playerId: string; reason: string };
  "update-available": { version: string; message: string };
  heartbeat: Record<string, never>;
}

export const usePlayerStore = defineStore("player", () => {
  // State
  const currentItem = ref<QueueItemResponse | null>(null);
  const playerState = ref<PlayerState>("Idle");
  const queueMode = ref<QueueMode>("Sequential");
  const position = ref(0);
  const duration = ref(0);
  const isKillSwitchActive = ref(false);
  const lastUpdate = ref<Date>(new Date());
  const lastError = ref<string | null>(null);

  // Computed
  const isPlaying = computed(() => playerState.value === "Playing");
  const isPaused = computed(() => playerState.value === "Paused");
  const isStopped = computed(() => playerState.value === "Stopped");
  const isIdle = computed(() => playerState.value === "Idle");
  const progress = computed(() =>
    duration.value > 0 ? (position.value / duration.value) * 100 : 0,
  );

  // Callbacks for external consumers (toast, query invalidation)
  type EventCallback = (event: SSEEventType, data: unknown) => void;
  const listeners: EventCallback[] = [];

  function onSSEEvent(callback: EventCallback) {
    listeners.push(callback);
    return () => {
      const idx = listeners.indexOf(callback);
      if (idx >= 0) listeners.splice(idx, 1);
    };
  }

  function notifyListeners(event: SSEEventType, data: unknown) {
    for (const cb of listeners) cb(event, data);
  }

  /** Called by useSSE's onEvent callback */
  function handleSSEEvent(event: string, data: unknown) {
    const sseEvent = event as SSEEventType;
    lastUpdate.value = new Date();

    switch (sseEvent) {
      case "state-changed": {
        const d = data as SSEEventPayloads["state-changed"];
        playerState.value = d.state;
        if (d.state !== "Idle") lastError.value = null;
        break;
      }
      case "track-changed": {
        const d = data as SSEEventPayloads["track-changed"];
        currentItem.value = d.item;
        position.value = d.position;
        lastError.value = null;
        break;
      }
      case "position-updated": {
        const d = data as SSEEventPayloads["position-updated"];
        position.value = d.position;
        duration.value = d.duration;
        break;
      }
      case "kill-switch-toggled": {
        const d = data as SSEEventPayloads["kill-switch-toggled"];
        isKillSwitchActive.value = d.active;
        break;
      }
      case "playback-error": {
        const d = data as SSEEventPayloads["playback-error"];
        lastError.value = d.error;
        break;
      }
      case "queue-updated":
      case "item-added":
      case "policy-changed":
      case "player-offline":
      case "player-online":
      case "player-disconnected":
      case "update-available":
        // Pass through to listeners (toast + query invalidation)
        break;
      case "heartbeat":
        return; // No state change, no notification
    }

    notifyListeners(sseEvent, data);
  }

  /** Handle polling fallback data (from /now-playing) */
  function handlePollState(data: unknown) {
    const d = data as {
      state?: PlayerState;
      currentItem?: QueueItemResponse;
      position?: number;
      duration?: number;
    };
    if (d.state) playerState.value = d.state;
    if (d.currentItem !== undefined) currentItem.value = d.currentItem ?? null;
    if (d.position !== undefined) position.value = d.position;
    if (d.duration !== undefined) duration.value = d.duration;
    lastUpdate.value = new Date();
  }

  function reset() {
    currentItem.value = null;
    playerState.value = "Idle";
    position.value = 0;
    duration.value = 0;
    isKillSwitchActive.value = false;
    lastError.value = null;
  }

  return {
    // State
    currentItem,
    playerState,
    queueMode,
    position,
    duration,
    isKillSwitchActive,
    lastUpdate,
    lastError,
    // Computed
    isPlaying,
    isPaused,
    isStopped,
    isIdle,
    progress,
    // Actions
    handleSSEEvent,
    handlePollState,
    onSSEEvent,
    reset,
  };
});
