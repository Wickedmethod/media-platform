<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from "vue";
import { usePlayerStore } from "@/stores/player";
import { config } from "@/config";

const playerStore = usePlayerStore();
const playerEl = ref<HTMLDivElement>();
let ytPlayer: YT.Player | null = null;
let positionInterval: ReturnType<typeof setInterval> | null = null;

const emit = defineEmits<{
  trackStart: [];
  trackEnd: [];
  error: [code: number];
}>();

function extractVideoId(url: string): string {
  try {
    const u = new URL(url);
    return u.searchParams.get("v") ?? u.pathname.split("/").pop() ?? "";
  } catch {
    return url;
  }
}

function loadYouTubeApi(): Promise<void> {
  if (window.YT?.Player) return Promise.resolve();
  return new Promise((resolve) => {
    const tag = document.createElement("script");
    tag.src = "https://www.youtube.com/iframe_api";
    window.onYouTubeIframeAPIReady = () => resolve();
    document.head.appendChild(tag);
  });
}

function initPlayer() {
  if (!playerEl.value) return;
  ytPlayer = new YT.Player(playerEl.value, {
    height: "100%",
    width: "100%",
    playerVars: {
      autoplay: 1,
      controls: 0,
      modestbranding: 1,
      rel: 0,
      iv_load_policy: 3,
      fs: 0,
      disablekb: 1,
      playsinline: 1,
    },
    events: {
      onStateChange: (e) => {
        if (e.data === YT.PlayerState.ENDED) {
          emit("trackEnd");
          reportTrackEnd();
        }
      },
      onError: (e) => {
        emit("error", e.data);
        reportPlaybackError(e.data);
      },
    },
  });
}

async function reportTrackEnd() {
  try {
    await fetch(`${config.apiBaseUrl}/player/skip`, { method: "POST" });
  } catch {
    /* ignore */
  }
}

async function reportPlaybackError(code: number) {
  try {
    await fetch(`${config.apiBaseUrl}/player/error`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ reason: `YouTube error ${code}` }),
    });
  } catch {
    /* ignore */
  }
}

function startPositionReporting() {
  stopPositionReporting();
  positionInterval = setInterval(async () => {
    if (ytPlayer?.getPlayerState() === YT.PlayerState.PLAYING) {
      const pos = Math.floor(ytPlayer.getCurrentTime());
      try {
        await fetch(`${config.apiBaseUrl}/player/position`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ positionSeconds: pos }),
        });
      } catch {
        /* ignore */
      }
    }
  }, 5000);
}

function stopPositionReporting() {
  if (positionInterval) {
    clearInterval(positionInterval);
    positionInterval = null;
  }
}

// Watch for track changes from SSE → load video
watch(
  () => playerStore.currentItem,
  (item) => {
    if (item && ytPlayer) {
      const videoId = extractVideoId(item.url);
      ytPlayer.loadVideoById({ videoId, startSeconds: playerStore.position });
      emit("trackStart");
    }
  },
);

// Watch for play/pause/stop commands
watch(
  () => playerStore.playerState,
  (state) => {
    if (!ytPlayer) return;
    if (state === "Paused") ytPlayer.pauseVideo();
    if (state === "Playing") ytPlayer.playVideo();
    if (state === "Stopped") ytPlayer.stopVideo();
  },
);

onMounted(async () => {
  await loadYouTubeApi();
  initPlayer();
  startPositionReporting();
});

onUnmounted(() => {
  stopPositionReporting();
  ytPlayer?.destroy();
  ytPlayer = null;
});
</script>

<template>
  <div ref="playerEl" class="absolute inset-0 bg-black" />
</template>
