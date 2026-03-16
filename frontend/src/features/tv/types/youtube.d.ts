/** YouTube IFrame API type declarations for TV player */

declare namespace YT {
  enum PlayerState {
    UNSTARTED = -1,
    ENDED = 0,
    PLAYING = 1,
    PAUSED = 2,
    BUFFERING = 3,
    CUED = 5,
  }

  interface PlayerOptions {
    height?: string | number;
    width?: string | number;
    videoId?: string;
    playerVars?: Record<string, unknown>;
    events?: {
      onReady?: (event: { target: Player }) => void;
      onStateChange?: (event: { data: PlayerState }) => void;
      onError?: (event: { data: number }) => void;
    };
  }

  class Player {
    constructor(element: HTMLElement | string, options: PlayerOptions);
    loadVideoById(opts: { videoId: string; startSeconds?: number }): void;
    playVideo(): void;
    pauseVideo(): void;
    stopVideo(): void;
    getCurrentTime(): number;
    getDuration(): number;
    getPlayerState(): PlayerState;
    destroy(): void;
  }
}

interface Window {
  onYouTubeIframeAPIReady?: () => void;
  YT?: typeof YT;
}
