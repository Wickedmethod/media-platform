import { ref, computed } from "vue";
import { usePlay, usePause, useSkip, useStop } from "@/generated/player/player";
import { useToast } from "@/composables/useToast";

export type PlayerCommand = "play" | "pause" | "skip" | "stop";

export function usePlayerCommands() {
  const toast = useToast();
  const pendingCommand = ref<PlayerCommand | null>(null);
  const isDisabled = computed(() => pendingCommand.value !== null);

  function onSettled() {
    pendingCommand.value = null;
  }

  const playMutation = usePlay({
    mutation: {
      onMutate: () => { pendingCommand.value = "play"; },
      onSettled,
      onError: () => toast.error("Play failed"),
    },
  });

  const pauseMutation = usePause({
    mutation: {
      onMutate: () => { pendingCommand.value = "pause"; },
      onSettled,
      onError: () => toast.error("Pause failed"),
    },
  });

  const skipMutation = useSkip({
    mutation: {
      onMutate: () => { pendingCommand.value = "skip"; },
      onSettled,
      onError: () => toast.error("Skip failed"),
    },
  });

  const stopMutation = useStop({
    mutation: {
      onMutate: () => { pendingCommand.value = "stop"; },
      onSettled,
      onError: () => toast.error("Stop failed"),
    },
  });

  function play() {
    if (!isDisabled.value) playMutation.mutate();
  }

  function pause() {
    if (!isDisabled.value) pauseMutation.mutate();
  }

  function skip() {
    if (!isDisabled.value) skipMutation.mutate();
  }

  function stop() {
    if (!isDisabled.value) stopMutation.mutate();
  }

  return {
    play,
    pause,
    skip,
    stop,
    pendingCommand,
    isDisabled,
  };
}
