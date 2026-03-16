<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from "vue";
import { config } from "@/config";
import type { SearchResult } from "@/lib/invidious";

const emit = defineEmits<{
  close: [];
  added: [title: string];
}>();

// --- Keyboard layouts ---
const LAYOUTS = {
  alpha: [
    ["Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P"],
    ["A", "S", "D", "F", "G", "H", "J", "K", "L", "'"],
    ["⇧", "Z", "X", "C", "V", "B", "N", "M", "⌫"],
    ["123", "SPACE", "Search"],
  ],
  numeric: [
    ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"],
    ["-", "=", ".", ",", "!", "?", "@", "#", "⌫"],
    ["ABC", "SPACE", "Search"],
  ],
} as const;

type LayoutName = keyof typeof LAYOUTS;
type Zone = "results" | "keyboard";

// --- State ---
const query = ref("");
const results = ref<SearchResult[]>([]);
const isSearching = ref(false);
const addedId = ref<string | null>(null);

// Keyboard state
const currentLayout = ref<LayoutName>("alpha");
const isShift = ref(false);
const focusRow = ref(0);
const focusCol = ref(0);

// Zone navigation
const currentZone = ref<Zone>("keyboard");
const resultFocusIndex = ref(0);

// Search debounce
let searchTimer: ReturnType<typeof setTimeout> | null = null;

const currentKeys = computed(() => LAYOUTS[currentLayout.value]);

// --- Keyboard actions ---
function selectKey() {
  const row = currentKeys.value[focusRow.value];
  if (!row) return;
  const key = row[focusCol.value];
  if (!key) return;

  switch (key) {
    case "⌫":
      query.value = query.value.slice(0, -1);
      break;
    case "SPACE":
      query.value += " ";
      break;
    case "⇧":
      isShift.value = !isShift.value;
      break;
    case "Search":
      triggerSearch();
      break;
    case "123":
      currentLayout.value = "numeric";
      focusRow.value = 0;
      focusCol.value = Math.min(focusCol.value, LAYOUTS.numeric[0].length - 1);
      break;
    case "ABC":
      currentLayout.value = "alpha";
      focusRow.value = 0;
      focusCol.value = Math.min(focusCol.value, LAYOUTS.alpha[0].length - 1);
      break;
    default: {
      const char = isShift.value ? key : key.toLowerCase();
      query.value += char;
      if (isShift.value) isShift.value = false;
      break;
    }
  }
}

function navigateKeyboard(direction: "up" | "down" | "left" | "right") {
  const layout = currentKeys.value;

  if (currentZone.value === "results") {
    if (direction === "left") {
      resultFocusIndex.value = Math.max(0, resultFocusIndex.value - 1);
    } else if (direction === "right") {
      resultFocusIndex.value = Math.min(
        results.value.length - 1,
        resultFocusIndex.value + 1,
      );
    } else if (direction === "down") {
      currentZone.value = "keyboard";
    }
    return;
  }

  // Keyboard zone
  switch (direction) {
    case "up":
      if (focusRow.value === 0 && results.value.length > 0) {
        currentZone.value = "results";
        resultFocusIndex.value = 0;
      } else {
        focusRow.value = Math.max(0, focusRow.value - 1);
        focusCol.value = Math.min(
          focusCol.value,
          layout[focusRow.value].length - 1,
        );
      }
      break;
    case "down":
      focusRow.value = Math.min(layout.length - 1, focusRow.value + 1);
      focusCol.value = Math.min(
        focusCol.value,
        layout[focusRow.value].length - 1,
      );
      break;
    case "left":
      focusCol.value = Math.max(0, focusCol.value - 1);
      break;
    case "right":
      focusCol.value = Math.min(
        layout[focusRow.value].length - 1,
        focusCol.value + 1,
      );
      break;
  }
}

function handleSelect() {
  if (currentZone.value === "results") {
    const result = results.value[resultFocusIndex.value];
    if (result) addToQueue(result);
  } else {
    selectKey();
  }
}

// --- Search ---
async function triggerSearch() {
  const q = query.value.trim();
  if (q.length < 2) return;

  isSearching.value = true;
  try {
    const url = `${config.apiBaseUrl}/search/youtube?q=${encodeURIComponent(q)}`;
    const res = await fetch(url);
    if (res.ok) {
      const data = (await res.json()) as SearchResult[];
      results.value = data.slice(0, 5);
    }
  } catch {
    // Search failed — keep existing results
  } finally {
    isSearching.value = false;
  }
}

// Auto-search with 500ms debounce
watch(query, (val) => {
  if (searchTimer) clearTimeout(searchTimer);
  const trimmed = val.trim();
  if (trimmed.length < 2) {
    results.value = [];
    return;
  }
  searchTimer = setTimeout(() => {
    triggerSearch();
  }, 500);
});

// --- Queue ---
async function addToQueue(result: SearchResult) {
  try {
    const res = await fetch(`${config.apiBaseUrl}/queue`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ url: result.youtubeUrl, title: result.title }),
    });
    if (res.ok) {
      addedId.value = result.videoId;
      setTimeout(() => {
        addedId.value = null;
      }, 2000);
      emit("added", result.title);
    }
  } catch {
    // Failed silently — user can retry
  }
}

function formatDuration(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, "0")}`;
}

// --- Keyboard input handler ---
function onKeyDown(e: KeyboardEvent) {
  switch (e.key) {
    case "ArrowUp":
      navigateKeyboard("up");
      e.preventDefault();
      break;
    case "ArrowDown":
      navigateKeyboard("down");
      e.preventDefault();
      break;
    case "ArrowLeft":
      navigateKeyboard("left");
      e.preventDefault();
      break;
    case "ArrowRight":
      navigateKeyboard("right");
      e.preventDefault();
      break;
    case "Enter":
    case " ":
      handleSelect();
      e.preventDefault();
      break;
    case "Backspace":
    case "Escape":
      if (query.value.length > 0 && e.key === "Backspace") {
        query.value = query.value.slice(0, -1);
      } else {
        emit("close");
      }
      e.preventDefault();
      break;
  }
}

onMounted(() => {
  window.addEventListener("keydown", onKeyDown);
});

onUnmounted(() => {
  window.removeEventListener("keydown", onKeyDown);
  if (searchTimer) clearTimeout(searchTimer);
});

function keyLabel(key: string): string {
  if (key === "SPACE") return "Space";
  return key;
}

function isSpecialKey(key: string): boolean {
  return ["⇧", "⌫", "123", "ABC", "SPACE", "Search"].includes(key);
}
</script>

<template>
  <div
    class="absolute inset-0 z-40 flex flex-col bg-black/95 p-8"
  >
    <!-- Search input display -->
    <div class="mb-6 flex items-center gap-4">
      <span class="text-3xl">🔍</span>
      <div
        class="flex-1 rounded-lg border-2 border-white/20 bg-white/5 px-6 py-4 text-2xl text-white"
      >
        {{ query }}<span class="animate-pulse text-pink-500">█</span>
      </div>
    </div>

    <!-- Results row -->
    <div v-if="results.length > 0" class="mb-6 flex gap-4 overflow-x-auto">
      <button
        v-for="(result, idx) in results"
        :key="result.videoId"
        class="flex w-64 flex-shrink-0 flex-col rounded-xl border-2 bg-white/5 p-3 text-left transition-all duration-150"
        :class="
          currentZone === 'results' && resultFocusIndex === idx
            ? 'scale-105 border-pink-500 bg-pink-500/20 shadow-lg shadow-pink-500/30'
            : 'border-transparent'
        "
      >
        <div
          v-if="addedId === result.videoId"
          class="flex h-full items-center justify-center text-xl text-green-400"
        >
          ✓ Added
        </div>
        <template v-else>
          <img
            :src="result.thumbnailUrl"
            :alt="result.title"
            class="mb-2 h-36 w-full rounded-lg object-cover"
          />
          <p class="line-clamp-2 text-sm font-medium text-white">
            {{ result.title }}
          </p>
          <p class="mt-1 text-xs text-white/50">
            {{ result.channel }} · {{ formatDuration(result.duration) }}
          </p>
        </template>
      </button>
    </div>

    <!-- Loading indicator -->
    <div
      v-else-if="isSearching"
      class="mb-6 flex h-20 items-center justify-center"
    >
      <span class="text-lg text-white/40">Searching…</span>
    </div>

    <!-- Keyboard -->
    <div class="mt-auto flex flex-col items-center gap-2">
      <div
        v-for="(row, rowIdx) in currentKeys"
        :key="rowIdx"
        class="flex gap-2"
      >
        <button
          v-for="(key, colIdx) in row"
          :key="key"
          class="flex items-center justify-center rounded-lg border-2 text-xl text-white/80 transition-all duration-150"
          :class="[
            currentZone === 'keyboard' &&
            focusRow === rowIdx &&
            focusCol === colIdx
              ? 'scale-110 border-pink-500 bg-pink-500/20 shadow-lg shadow-pink-500/30'
              : 'border-transparent bg-white/5',
            isSpecialKey(key) ? 'min-w-20 px-5 py-4' : 'h-14 w-14',
            key === 'SPACE' ? 'min-w-64' : '',
            key === '⇧' && isShift ? 'bg-pink-500/30 text-pink-300' : '',
          ]"
        >
          {{ keyLabel(key) }}
        </button>
      </div>
    </div>

    <!-- Hint -->
    <p class="mt-4 text-center text-sm text-white/30">
      Arrow keys to navigate · OK to select · Back to close
    </p>
  </div>
</template>
