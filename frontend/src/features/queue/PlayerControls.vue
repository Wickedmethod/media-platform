<script setup lang="ts">
import { Play, Pause, SkipForward, Square, Loader2 } from "lucide-vue-next";
import { usePlayerStore } from "@/stores/player";
import { Button } from "@/shared/components/ui/button";
import type { PlayerCommand } from "@/composables/usePlayerCommands";

defineProps<{
  pendingCommand?: PlayerCommand | null;
  isDisabled?: boolean;
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
      :disabled="isDisabled"
      @click="emit('pause')"
    >
      <Loader2 v-if="pendingCommand === 'pause'" class="mr-1 h-4 w-4 animate-spin" />
      <Pause v-else class="mr-1 h-4 w-4" />
      Pause
    </Button>
    <Button
      v-else
      variant="outline"
      size="sm"
      :disabled="isDisabled"
      @click="emit('play')"
    >
      <Loader2 v-if="pendingCommand === 'play'" class="mr-1 h-4 w-4 animate-spin" />
      <Play v-else class="mr-1 h-4 w-4" />
      Play
    </Button>

    <!-- Skip -->
    <Button
      variant="outline"
      size="sm"
      :disabled="isDisabled"
      @click="emit('skip')"
    >
      <Loader2 v-if="pendingCommand === 'skip'" class="mr-1 h-4 w-4 animate-spin" />
      <SkipForward v-else class="mr-1 h-4 w-4" />
      Skip
    </Button>

    <!-- Stop -->
    <Button
      variant="outline"
      size="sm"
      :disabled="isDisabled"
      @click="emit('stop')"
    >
      <Loader2 v-if="pendingCommand === 'stop'" class="mr-1 h-4 w-4 animate-spin" />
      <Square v-else class="mr-1 h-4 w-4" />
      Stop
    </Button>
  </div>
</template>
