<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from "vue";

const props = defineProps<{
  videoUrl: string | null;
  startAt?: number;
  isPlaying: boolean;
}>();

const emit = defineEmits<{
  trackEnd: [];
  error: [code: number];
}>();

const playerEl = ref<HTMLDivElement>();
let ytPlayer: YT.Player | null = null;

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
    if (document.querySelector('script[src*="youtube.com/iframe_api"]')) {
      // Script already loading, wait for it
      const check = setInterval(() => {
        if (window.YT?.Player) {
          clearInterval(check);
          resolve();
        }
      }, 100);
      return;
    }
    const tag = document.createElement("script");
    tag.src = "https://www.youtube.com/iframe_api";
    const prev = window.onYouTubeIframeAPIReady;
    window.onYouTubeIframeAPIReady = () => {
      prev?.();
      resolve();
    };
    document.head.appendChild(tag);
  });
}

function initPlayer() {
  if (!playerEl.value) return;
  ytPlayer = new YT.Player(playerEl.value, {
    height: "1",
    width: "1",
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
        }
      },
      onError: (e) => {
        emit("error", e.data);
      },
    },
  });
}

watch(
  () => props.videoUrl,
  (url) => {
    if (url && ytPlayer) {
      const videoId = extractVideoId(url);
      ytPlayer.loadVideoById({ videoId, startSeconds: props.startAt ?? 0 });
    }
  },
);

watch(
  () => props.isPlaying,
  (playing) => {
    if (playing) ytPlayer?.playVideo();
    else ytPlayer?.pauseVideo();
  },
);

onMounted(async () => {
  await loadYouTubeApi();
  initPlayer();
});

onUnmounted(() => {
  ytPlayer?.destroy();
  ytPlayer = null;
});
</script>

<template>
  <div
    class="pointer-events-none fixed -left-[9999px] -top-[9999px] h-px w-px overflow-hidden"
  >
    <div ref="playerEl" />
  </div>
</template>
