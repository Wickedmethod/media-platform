import { onUnmounted } from "vue";

export interface PlaybackTimeoutOptions {
  /** Timeout for initial playback start (default: 30s) */
  initialTimeoutMs?: number;
  /** Timeout for retry playback start (default: 15s) */
  retryTimeoutMs?: number;
  /** Called when playback hasn't started within timeout */
  onTimeout: (videoId: string, phase: "initial" | "retry") => void;
  /** Called when retry also times out — skip to next */
  onSkip: (videoId: string) => void;
}

export function usePlaybackTimeout(options: PlaybackTimeoutOptions) {
  const {
    initialTimeoutMs = 30_000,
    retryTimeoutMs = 15_000,
    onTimeout,
    onSkip,
  } = options;

  let timer: ReturnType<typeof setTimeout> | null = null;

  function startWatching(videoId: string) {
    clearTimer();

    timer = setTimeout(() => {
      onTimeout(videoId, "initial");
      // Caller should reload video — start retry timer
      timer = setTimeout(() => {
        onSkip(videoId);
      }, retryTimeoutMs);
    }, initialTimeoutMs);
  }

  function playbackStarted() {
    clearTimer();
  }

  function clearTimer() {
    if (timer) {
      clearTimeout(timer);
      timer = null;
    }
  }

  function stop() {
    clearTimer();
  }

  onUnmounted(stop);

  return { startWatching, playbackStarted, stop };
}
