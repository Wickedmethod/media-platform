<script setup lang="ts">
import { Play, Pause, SkipForward, Square, Loader2 } from "lucide-vue-next";
import { usePlayerStore } from "@/stores/player";
import { Button } from "@/shared/components/ui/button";

defineProps<{
  playLoading?: boolean;
  pauseLoading?: boolean;
  skipLoading?: boolean;
  stopLoading?: boolean;
}>();

const emit = defineEmits<{
  play: [];
  pause: [];
  skip: [];
  stop: [];
}>();

const player = usePlayerStore();
</script>

<template>
  <div class="flex items-center gap-2">
    <!-- Play / Pause -->
    <Button
      v-if="player.isPlaying"
      variant="outline"
      size="sm"
      :disabled="pauseLoading"
      @click="emit('pause')"
    >
      <Loader2 v-if="pauseLoading" class="mr-1 h-4 w-4 animate-spin" />
      <Pause v-else class="mr-1 h-4 w-4" />
      Pause
    </Button>
    <Button
      v-else
      variant="outline"
      size="sm"
      :disabled="playLoading"
      @click="emit('play')"
    >
      <Loader2 v-if="playLoading" class="mr-1 h-4 w-4 animate-spin" />
      <Play v-else class="mr-1 h-4 w-4" />
      Play
    </Button>

    <!-- Skip -->
    <Button
      variant="outline"
      size="sm"
      :disabled="skipLoading"
      @click="emit('skip')"
    >
      <Loader2 v-if="skipLoading" class="mr-1 h-4 w-4 animate-spin" />
      <SkipForward v-else class="mr-1 h-4 w-4" />
      Skip
    </Button>

    <!-- Stop -->
    <Button
      variant="outline"
      size="sm"
      :disabled="stopLoading"
      @click="emit('stop')"
    >
      <Loader2 v-if="stopLoading" class="mr-1 h-4 w-4 animate-spin" />
      <Square v-else class="mr-1 h-4 w-4" />
      Stop
    </Button>
  </div>
</template>
