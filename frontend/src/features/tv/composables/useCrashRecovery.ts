import { onMounted, onUnmounted, watch } from "vue";
import { usePlayerStore, type PlayerState } from "@/stores/player";
import { config } from "@/config";

const STORAGE_KEY = "tv-state";
const PERSIST_INTERVAL_MS = 10_000;

interface PersistedState {
  lastVideoId: string | null;
  lastPosition: number;
  lastState: PlayerState;
  timestamp: number;
}

function loadPersistedState(): PersistedState | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw) as PersistedState;
  } catch {
    return null;
  }
}

function savePersistedState(state: PersistedState) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch {
    // Storage full or unavailable — silently ignore
  }
}

/**
 * Crash recovery composable for the TV kiosk. Persists minimal state
 * to localStorage every 10s while playing, and on startup reconciles
 * local state with the server via /sync. Also installs a global error
 * handler to attempt recovery on JS crashes.
 */
export function useCrashRecovery() {
  const player = usePlayerStore();
  let persistTimer: ReturnType<typeof setInterval> | null = null;
  let errorHandlerInstalled = false;
  let originalOnError: OnErrorEventHandler = null;

  function persistState() {
    const state: PersistedState = {
      lastVideoId: player.currentItem?.url?.match(/[?&]v=([^&]+)/)?.[1]
        ?? player.currentItem?.id ?? null,
      lastPosition: player.position,
      lastState: player.playerState,
      timestamp: Date.now(),
    };
    savePersistedState(state);
  }

  function startPersisting() {
    stopPersisting();
    persistTimer = setInterval(persistState, PERSIST_INTERVAL_MS);
  }

  function stopPersisting() {
    if (persistTimer) {
      clearInterval(persistTimer);
      persistTimer = null;
    }
  }

  /** Fetch server state and apply to store */
  async function reconcile(): Promise<void> {
    try {
      const res = await fetch(`${config.apiBaseUrl}/sync`);
      if (!res.ok) return;
      const snapshot = await res.json();
      player.applySnapshot(snapshot);
    } catch {
      // Server unreachable — SSE will handle state once connected
    }
  }

  function handleGlobalError(
    _event: Event | string,
    _source?: string,
    _lineno?: number,
    _colno?: number,
    _error?: Error,
  ) {
    // Persist current state before potential crash cascade
    persistState();
  }

  onMounted(async () => {
    // Install global error handler
    originalOnError = window.onerror;
    window.onerror = handleGlobalError;
    errorHandlerInstalled = true;

    // Initial reconciliation — fetch server state
    await reconcile();

    // Start periodic persistence if currently playing
    if (player.isPlaying) {
      startPersisting();
    }
  });

  // Start/stop persistence based on player state
  watch(
    () => player.playerState,
    (state) => {
      if (state === "Playing") {
        startPersisting();
      } else {
        // Persist final state before stopping timer
        persistState();
        stopPersisting();
      }
    },
  );

  onUnmounted(() => {
    stopPersisting();
    if (errorHandlerInstalled) {
      window.onerror = originalOnError;
    }
  });

  return { reconcile, persistState };
}
