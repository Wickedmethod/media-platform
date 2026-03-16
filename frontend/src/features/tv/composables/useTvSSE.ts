import { watch } from "vue";
import { useSSE } from "@/composables/useSSE";
import { usePlayerStore } from "@/stores/player";
import { config } from "@/config";

/**
 * TV-specific SSE composable: infinite reconnect, heartbeat detection,
 * and full state recovery via /sync on every reconnect.
 */
export function useTvSSE() {
  const player = usePlayerStore();

  const sse = useSSE({
    url: config.apiEventsUrl,
    maxReconnectAttempts: Infinity, // TV never gives up
    heartbeatTimeoutMs: 60_000,
    onEvent: (event, data) => {
      if (event === "poll-state") {
        player.handlePollState(data);
      } else {
        player.handleSSEEvent(event, data);
      }
    },
    onError: () => {
      // Error handled internally by useSSE with backoff
    },
  });

  // Sync SSE status to player store
  watch(
    [sse.connected, sse.isReconnecting],
    ([connected, reconnecting]) => {
      player.setSseStatus(connected, reconnecting);
    },
    { immediate: true },
  );

  // Recover full state from /sync whenever we reconnect
  watch(sse.connected, async (isConnected, wasConnected) => {
    if (isConnected && wasConnected === false) {
      await recoverState();
    }
  });

  async function recoverState() {
    try {
      const res = await fetch(`${config.apiBaseUrl}/sync`);
      if (res.ok) {
        const snapshot = await res.json();
        player.applySnapshot(snapshot);
      }
    } catch {
      // Recovery failed — will get state from next SSE events
    }
  }

  return {
    ...sse,
    recoverState,
  };
}
