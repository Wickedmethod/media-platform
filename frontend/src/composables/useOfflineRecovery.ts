import { watch } from "vue";
import { useQueryClient } from "@tanstack/vue-query";
import { usePlayerStore } from "@/stores/player";

/**
 * Invalidates all TanStack queries when SSE reconnects so views show fresh data.
 * Also exposes a manual retry that refetches all active queries.
 */
export function useOfflineRecovery() {
  const player = usePlayerStore();
  const queryClient = useQueryClient();
  let wasDisconnected = false;

  watch(
    () => player.sseConnected,
    (connected) => {
      if (!connected) {
        wasDisconnected = true;
      } else if (wasDisconnected) {
        wasDisconnected = false;
        queryClient.invalidateQueries();
      }
    },
  );

  function manualRetry() {
    queryClient.invalidateQueries();
  }

  return { manualRetry };
}
