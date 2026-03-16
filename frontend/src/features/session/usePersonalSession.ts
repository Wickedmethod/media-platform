import { ref, computed, onUnmounted } from "vue";
import { useAuthStore } from "@/stores/auth";
import { useToast } from "@/composables/useToast";
import {
  createPersonalSession,
  getMySession,
  addToSessionQueue,
  sessionPlayerCommand,
  endSession,
  getSessionEventsUrl,
  type SessionSnapshotResponse,
  type SessionPlaybackState,
  type SessionQueueItem,
} from "./api";

type SessionSSEEvent = "session-queue-updated" | "session-playback-state" | "session-ended" | "heartbeat";

const SESSION_SSE_EVENTS: readonly SessionSSEEvent[] = [
  "session-queue-updated",
  "session-playback-state",
  "session-ended",
  "heartbeat",
];

export function usePersonalSession() {
  const auth = useAuthStore();
  const toast = useToast();

  const sessionId = ref<string | null>(null);
  const queue = ref<SessionQueueItem[]>([]);
  const playback = ref<SessionPlaybackState>({
    state: "Idle",
    currentItem: null,
    startedAt: null,
    positionSeconds: 0,
    retryCount: 0,
    lastError: null,
  });
  const loading = ref(false);
  const active = computed(() => sessionId.value !== null);
  const isPlaying = computed(() => playback.value.state === "Playing");
  const currentItem = computed(() => playback.value.currentItem);

  let eventSource: EventSource | null = null;

  function deviceId(): string {
    const key = "media-personal-device-id";
    let id = localStorage.getItem(key);
    if (!id) {
      id = crypto.randomUUID();
      localStorage.setItem(key, id);
    }
    return id;
  }

  async function start() {
    if (active.value) return;
    loading.value = true;
    try {
      const session = await createPersonalSession(deviceId());
      sessionId.value = session.sessionId;
      await refresh();
      connectSSE();
    } catch {
      toast.error("Failed to start personal session");
    } finally {
      loading.value = false;
    }
  }

  async function refresh() {
    try {
      const snapshot = await getMySession();
      applySnapshot(snapshot);
    } catch {
      // session may not exist yet
    }
  }

  function applySnapshot(snapshot: SessionSnapshotResponse) {
    sessionId.value = snapshot.session.sessionId;
    queue.value = snapshot.queue;
    playback.value = snapshot.playback;
  }

  async function addToQueue(url: string, title: string) {
    if (!sessionId.value) return;
    try {
      await addToSessionQueue(sessionId.value, url, title);
      await refresh();
    } catch {
      toast.error("Failed to add to personal queue");
    }
  }

  async function play() {
    if (!sessionId.value) return;
    try {
      playback.value = await sessionPlayerCommand(sessionId.value, "play");
    } catch {
      toast.error("Play failed");
    }
  }

  async function pause() {
    if (!sessionId.value) return;
    try {
      playback.value = await sessionPlayerCommand(sessionId.value, "pause");
    } catch {
      toast.error("Pause failed");
    }
  }

  async function skip() {
    if (!sessionId.value) return;
    try {
      playback.value = await sessionPlayerCommand(sessionId.value, "skip");
      await refresh();
    } catch {
      toast.error("Skip failed");
    }
  }

  async function stop() {
    if (!sessionId.value) return;
    try {
      await endSession(sessionId.value);
      disconnectSSE();
      sessionId.value = null;
      queue.value = [];
      playback.value = {
        state: "Idle",
        currentItem: null,
        startedAt: null,
        positionSeconds: 0,
        retryCount: 0,
        lastError: null,
      };
    } catch {
      toast.error("Failed to end session");
    }
  }

  function connectSSE() {
    disconnectSSE();
    if (!sessionId.value) return;

    const url = getSessionEventsUrl(sessionId.value, auth.token ?? undefined);
    eventSource = new EventSource(url);

    for (const eventType of SESSION_SSE_EVENTS) {
      eventSource.addEventListener(eventType, (event: MessageEvent) => {
        try {
          const data = JSON.parse(event.data) as unknown;
          handleSSEEvent(eventType, data);
        } catch {
          // ignore
        }
      });
    }

    eventSource.onerror = () => {
      // auto-reconnect is handled by EventSource
    };
  }

  function handleSSEEvent(event: SessionSSEEvent, data: unknown) {
    switch (event) {
      case "session-queue-updated":
        refresh();
        break;
      case "session-playback-state":
        playback.value = data as SessionPlaybackState;
        break;
      case "session-ended":
        sessionId.value = null;
        queue.value = [];
        break;
    }
  }

  function disconnectSSE() {
    eventSource?.close();
    eventSource = null;
  }

  // Try to resume existing session on mount
  async function tryResume() {
    try {
      const snapshot = await getMySession();
      applySnapshot(snapshot);
      connectSSE();
    } catch {
      // no active session
    }
  }

  onUnmounted(disconnectSSE);

  return {
    sessionId,
    queue,
    playback,
    loading,
    active,
    isPlaying,
    currentItem,
    start,
    stop,
    addToQueue,
    play,
    pause,
    skip,
    refresh,
    tryResume,
  };
}
