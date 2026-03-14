import { computed, ref, watch } from "vue";
import { usePlayerStore } from "@/stores/player";

export type SSEConnectionState =
  | "connected"
  | "connecting"
  | "disconnected"
  | "reconnected";

export function useSSEStatus() {
  const player = usePlayerStore();
  const wasDisconnected = ref(false);
  const showReconnected = ref(false);
  let hideTimer: ReturnType<typeof setTimeout> | null = null;

  const state = computed<SSEConnectionState>(() => {
    if (showReconnected.value) return "reconnected";
    if (player.sseReconnecting) return "connecting";
    if (!player.sseConnected) return "disconnected";
    return "connected";
  });

  watch(
    () => player.sseConnected,
    (connected) => {
      if (!connected) {
        wasDisconnected.value = true;
        if (hideTimer) {
          clearTimeout(hideTimer);
          hideTimer = null;
        }
        showReconnected.value = false;
      } else if (wasDisconnected.value) {
        showReconnected.value = true;
        wasDisconnected.value = false;
        hideTimer = setTimeout(() => {
          showReconnected.value = false;
          hideTimer = null;
        }, 2000);
      }
    },
  );

  const message = computed(() => {
    switch (state.value) {
      case "connecting":
        return "Connecting...";
      case "disconnected":
        return "Offline — changes may not be visible";
      case "reconnected":
        return "Back online";
      default:
        return "";
    }
  });

  return { state, message };
}
