<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from "vue";
import { usePlayerStore } from "@/stores/player";
import { config } from "@/config";

const playerStore = usePlayerStore();
const emit = defineEmits<{ skip: [] }>();

const RETRY_DELAY_MS = 3_000;
const SKIP_DELAY_MS = 10_000;

/** YouTube error codes → user-friendly messages */
const ERROR_MESSAGES: Record<number, string> = {
  2: "Invalid video parameter",
  5: "HTML5 player error",
  100: "Video not found or removed",
  101: "This video can't be played on TV",
  150: "This video can't be played on TV",
};

interface ErrorInfo {
  code?: number;
  message: string;
  retryable: boolean;
}

function parseError(raw: string | null): ErrorInfo {
  if (!raw) return { message: "An unknown error occurred", retryable: false };
  // Check if it's a YouTube error code format: "YouTube error 150"
  const codeMatch = raw.match(/YouTube error (\d+)/) as RegExpMatchArray | null;
  if (codeMatch?.[1]) {
    const code = parseInt(codeMatch[1], 10);
    return {
      code,
      message: ERROR_MESSAGES[code] ?? `Playback error (code ${code})`,
      retryable: code === 2 || code === 5,
    };
  }
  // Network / generic errors are retryable
  const isNetwork = /network|timeout|unreachable|fetch/i.test(raw);
  return {
    message: raw,
    retryable: isNetwork,
  };
}

const errorInfo = computed(() => parseError(playerStore.lastError));
const retryCount = ref(0);
const isRetrying = ref(false);
const skipCountdown = ref(0);
const skipProgress = ref(0);
let countdownTimer: ReturnType<typeof setInterval> | null = null;
let retryTimer: ReturnType<typeof setTimeout> | null = null;

function startSkipCountdown() {
  const start = Date.now();
  skipCountdown.value = Math.ceil(SKIP_DELAY_MS / 1000);
  skipProgress.value = 0;

  countdownTimer = setInterval(() => {
    const elapsed = Date.now() - start;
    skipProgress.value = Math.min((elapsed / SKIP_DELAY_MS) * 100, 100);
    skipCountdown.value = Math.max(
      0,
      Math.ceil((SKIP_DELAY_MS - elapsed) / 1000),
    );
    if (elapsed >= SKIP_DELAY_MS) {
      clearTimers();
      skipToNext();
    }
  }, 100);
}

function skipToNext() {
  clearTimers();
  retryCount.value = 0;
  emit("skip");
  fetch(`${config.apiBaseUrl}/player/skip`, { method: "POST" }).catch(() => {});
}

function handleError() {
  // Retry once for transient errors
  if (errorInfo.value.retryable && retryCount.value < 1) {
    retryCount.value++;
    isRetrying.value = true;
    retryTimer = setTimeout(() => {
      isRetrying.value = false;
      // If still in error state after retry delay, show countdown
      if (playerStore.lastError) {
        startSkipCountdown();
      }
    }, RETRY_DELAY_MS);
    return;
  }

  startSkipCountdown();
}

function clearTimers() {
  if (countdownTimer) {
    clearInterval(countdownTimer);
    countdownTimer = null;
  }
  if (retryTimer) {
    clearTimeout(retryTimer);
    retryTimer = null;
  }
}

// Re-trigger on new errors
watch(
  () => playerStore.lastError,
  (err) => {
    if (err) {
      clearTimers();
      skipCountdown.value = 0;
      skipProgress.value = 0;
      handleError();
    }
  },
);

onMounted(() => {
  if (playerStore.lastError) handleError();
});

onUnmounted(clearTimers);

defineExpose({ skipToNext });
</script>

<template>
  <div
    class="flex h-screen w-screen flex-col items-center justify-center bg-[#0a0a1a] text-white"
  >
    <p class="text-6xl">⚠️</p>
    <h2 class="mt-4 text-2xl font-bold">Can't Play Video</h2>

    <p
      v-if="playerStore.currentItem"
      class="mt-3 max-w-md truncate text-lg text-white/70"
    >
      "{{ playerStore.currentItem.title }}"
    </p>

    <p class="mt-2 text-lg text-white/60">
      {{ errorInfo.message }}
    </p>

    <!-- Retry state -->
    <p v-if="isRetrying" class="mt-6 animate-pulse text-sm text-amber-400">
      Retrying…
    </p>

    <!-- Skip countdown -->
    <template v-else-if="skipCountdown > 0">
      <p class="mt-6 text-sm text-white/50">
        Skipping in {{ skipCountdown }}s…
      </p>
      <div class="mt-3 h-1.5 w-64 overflow-hidden rounded-full bg-white/20">
        <div
          class="h-full rounded-full bg-white/60 transition-[width] duration-100"
          :style="{ width: `${skipProgress}%` }"
        />
      </div>
      <p class="mt-4 text-xs text-white/30">Press → to skip now</p>
    </template>
  </div>
</template>
